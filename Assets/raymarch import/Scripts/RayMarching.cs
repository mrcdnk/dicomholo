using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class RayMarching : MonoBehaviour
{
	[SerializeField]
	[Header("Render in a lower resolution to increase performance.")]
	private int downscale = 1;
	[SerializeField]
	private LayerMask volumeLayer;

	[SerializeField]
	private Shader compositeShader;
	[SerializeField]
	private Shader renderFrontDepthShader;
	[SerializeField]
	private Shader renderBackDepthShader;
	[SerializeField]
	private Shader rayMarchShader;

	[SerializeField][Range(0, 2)]
	private float opacity = 1;

    [SerializeField][Range(50, 512)]
    public int stepcount = 128;

	[Header("Clipping planes percentage")]
	[SerializeField]
	private Vector4 clipDimensions = new Vector4(100, 100, 100, 0);

	private Material _rayMarchMaterial;
	private Material _compositeMaterial;
	private Camera _ppCamera;
	private Texture3D _volumeBuffer;

	private void Awake()
	{
		_rayMarchMaterial = new Material(rayMarchShader);
		_compositeMaterial = new Material(compositeShader);
	}

	private void OnDestroy()
	{
		if(_volumeBuffer != null)
		{
			Destroy(_volumeBuffer);
		}
	}

	[SerializeField]
	private Transform clipPlane;
	[SerializeField]
	private Transform cubeTarget;
	
	private void OnRenderImage(RenderTexture source, RenderTexture destination) 
	{
       
	    if (_volumeBuffer != null)
	    {
	        _rayMarchMaterial.SetTexture("_VolumeTex", _volumeBuffer);

	        var width = source.width / downscale;
	        var height = source.height / downscale;

	        if (_ppCamera == null)
	        {
	            var go = new GameObject("PPCamera");
	            _ppCamera = go.AddComponent<Camera>();
	            _ppCamera.enabled = false;
	        }

	        _ppCamera.CopyFrom(GetComponent<Camera>());
	        _ppCamera.clearFlags = CameraClearFlags.SolidColor;
	        _ppCamera.backgroundColor = Color.white;
	        _ppCamera.cullingMask = volumeLayer;

	        var frontDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat);
	        var backDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat);

	        var volumeTarget = RenderTexture.GetTemporary(width, height, 0);

	        // need to set this vector because unity bakes object that are non uniformily scaled
	        //TODO:FIX
	        //Shader.SetGlobalVector("_VolumeScale", cubeTarget.transform.localScale);

	        // Render depths
	        _ppCamera.targetTexture = frontDepth;
	        _ppCamera.RenderWithShader(renderFrontDepthShader, "RenderType");
	        _ppCamera.targetTexture = backDepth;
	        _ppCamera.RenderWithShader(renderBackDepthShader, "RenderType");

	        // Render volume
	        _rayMarchMaterial.SetTexture("_FrontTex", frontDepth);
	        _rayMarchMaterial.SetTexture("_BackTex", backDepth);

	        if (cubeTarget != null && clipPlane != null && clipPlane.gameObject.activeSelf)
	        {
	            var p = new Plane(
	                cubeTarget.InverseTransformDirection(clipPlane.transform.up),
	                cubeTarget.InverseTransformPoint(clipPlane.position));
	            _rayMarchMaterial.SetVector("_ClipPlane", new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance));
	        }
	        else
	        {
	            _rayMarchMaterial.SetVector("_ClipPlane", Vector4.zero);
	        }

	        _rayMarchMaterial.SetFloat("_Opacity", opacity); // Blending strength 
	        _rayMarchMaterial.SetVector("_ClipDims", clipDimensions / 100f); // Clip box
	        _rayMarchMaterial.SetFloat("_Steps", stepcount);
            _rayMarchMaterial.SetFloat("_StepSize", 1/(float)stepcount);


	        Graphics.Blit(null, volumeTarget, _rayMarchMaterial);

	        //Composite
	        _compositeMaterial.SetTexture("_BlendTex", volumeTarget);
	        Graphics.Blit(source, destination, _compositeMaterial);

	        RenderTexture.ReleaseTemporary(volumeTarget);
	        RenderTexture.ReleaseTemporary(frontDepth);
	        RenderTexture.ReleaseTemporary(backDepth);
	    }
	    else
	    {
	        Graphics.Blit(source, destination);

	    }
	}

    public void initVolume(Texture3D tex3D)
    {
        this._volumeBuffer = tex3D;
        _rayMarchMaterial.SetTexture("_VolumeTex", _volumeBuffer);
    }
}
