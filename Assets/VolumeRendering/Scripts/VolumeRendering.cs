using UnityEngine;

namespace VolumeRendering
{

    [RequireComponent (typeof(MeshRenderer), typeof(MeshFilter))]
    public class VolumeRendering : MonoBehaviour {

        [SerializeField] protected Shader shader;
        protected Material material;

        [SerializeField] Color color = Color.white;
        [Range(0.5f, 3f)] private float intensity = 1f;
        [Range(0f, 2f)] private float opacity = 1f;
        [Range(1, 256)] private int stepCount = 128;
        [Range(0f, 1f)] public float sliceXMin = 0.0f, sliceXMax = 1.0f;
        [Range(0f, 1f)] public float sliceYMin = 0.0f, sliceYMax = 1.0f;
        [Range(0f, 1f)] public float sliceZMin = 0.0f, sliceZMax = 1.0f;
        private Quaternion axis = Quaternion.identity;

        public Transform AxisRoot;

        private Texture3D volume;

        public float Opacity
        {
            get { return opacity; }
            set
            {
                opacity = value;
                
                if(material)
                    material.SetFloat("_Opacity", Opacity);
            }
        }

        public float Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;

                if(material)
                    material.SetFloat("_Intensity", Intensity);
            }
        }

        public int StepCount
        {
            get { return stepCount; }
            set
            {
                stepCount = value;
                if (material)
                    material.SetInt("_StepCount", stepCount);
            }
        }

        protected virtual void Start()
        {
            material = new Material(shader);
            GetComponent<MeshFilter>().sharedMesh = Build();
            GetComponent<MeshRenderer>().sharedMaterial = material;

            material.SetColor("_Color", color);
            material.SetFloat("_Intensity", Intensity);
            material.SetFloat("_Opacity", Opacity);
            material.SetInt("_StepCount", StepCount);
            material.SetVector("_SliceMin", new Vector3(sliceXMin, sliceYMin, sliceZMin));
            material.SetVector("_SliceMax", new Vector3(sliceXMax, sliceYMax, sliceZMax));
            material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(axis));
        }

        private void Update()
        {
            Quaternion correct = new Quaternion(-AxisRoot.transform.localRotation.x, -AxisRoot.transform.localRotation.z, AxisRoot.transform.localRotation.y, AxisRoot.transform.localRotation.w);

            material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(correct));
        }

        public void SliceMinMaxChanged()
        {
            if (!material) return;
            material.SetVector("_SliceMin", new Vector3(sliceXMin, sliceYMin, sliceZMin));
            material.SetVector("_SliceMax", new Vector3(sliceXMax, sliceYMax, sliceZMax));
        }

        private Mesh Build() {
            var vertices = new Vector3[] {
                new Vector3 (-0.5f, -0.5f, -0.5f),
                new Vector3 ( 0.5f, -0.5f, -0.5f),
                new Vector3 ( 0.5f,  0.5f, -0.5f),
                new Vector3 (-0.5f,  0.5f, -0.5f),
                new Vector3 (-0.5f,  0.5f,  0.5f),
                new Vector3 ( 0.5f,  0.5f,  0.5f),
                new Vector3 ( 0.5f, -0.5f,  0.5f),
                new Vector3 (-0.5f, -0.5f,  0.5f),
            };
            var triangles = new int[] {
                0, 2, 1,
                0, 3, 2,
                2, 3, 4,
                2, 4, 5,
                1, 2, 5,
                1, 5, 6,
                0, 7, 4,
                0, 4, 3,
                5, 4, 7,
                5, 7, 6,
                0, 6, 7,
                0, 1, 6
            };

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            return mesh;
        }

        private void OnValidate()
        {
            Constrain(ref sliceXMin, ref sliceXMax);
            Constrain(ref sliceYMin, ref sliceYMax);
            Constrain(ref sliceZMin, ref sliceZMax);

            if (material)
            {
                material.SetColor("_Color", color);
                material.SetFloat("_Intensity", Intensity);
                material.SetFloat("_Opacity", Opacity);
                material.SetInt("_StepCount", StepCount);
                material.SetVector("_SliceMin", new Vector3(sliceXMin, sliceYMin, sliceZMin));
                material.SetVector("_SliceMax", new Vector3(sliceXMax, sliceYMax, sliceZMax));
                material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(axis));
            }
        }

        private void Constrain (ref float min, ref float max)
        {
            const float threshold = 1/256f;
            if(min > max - threshold)
            {
                min = max - threshold;
            } else if(max < min + threshold)
            {
                max = min + threshold;
            }
        }

        private void OnDestroy()
        {
            Destroy(material);
        }

        public void SetVolume(Texture3D texture3D)
        {
            material.SetTexture("_Volume", texture3D);
        }
       
    }

}


