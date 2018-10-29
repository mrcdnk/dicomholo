using System.Collections;
using System.Collections.Generic;
using DICOMData;
using UnityEngine;

public class OrthoSlices : MonoBehaviour
{
    private ImageStack imageStack;

    private Texture3D texture3D;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void initialize(ImageStack stack)
    {
        this.imageStack = stack;

        int[,,] data = imageStack.GetData();

        Color[] colorArray = new Color[data.GetLength(0) * data.GetLength(1) * data.GetLength(2)];
        texture3D = new Texture3D(data.GetLength(0), data.GetLength(1), data.GetLength(2), TextureFormat.RGBA32, true);       
        texture3D.SetPixels(colorArray);
        texture3D.Apply();
    }
}
