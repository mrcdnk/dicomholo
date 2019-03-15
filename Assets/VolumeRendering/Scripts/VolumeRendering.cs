using UnityEngine;

namespace VolumeRendering
{

    [RequireComponent (typeof(MeshRenderer), typeof(MeshFilter))]
    public class VolumeRendering : MonoBehaviour {

        public Transform AxisRoot;

        [Range(0f, 1f)] public float SliceXMin = 0.0f, SliceXMax = 1.0f;
        [Range(0f, 1f)] public float SliceYMin = 0.0f, SliceYMax = 1.0f;
        [Range(0f, 1f)] public float SliceZMin = 0.0f, SliceZMax = 1.0f;

        [SerializeField] protected Shader Shader;
        protected Material Material;

        [Range(0.5f, 3f)] private float _intensity = 1f;
        [Range(0f, 2f)] private float _opacity = 1f;
        [Range(1, 256)] private int _stepCount = 128;

        private Texture3D _volume;

        private readonly Quaternion _axis = Quaternion.identity;

        public float Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                
                if(Material)
                    Material.SetFloat("_Opacity", Opacity);
            }
        }

        public float Intensity
        {
            get { return _intensity; }
            set
            {
                _intensity = value;

                if(Material)
                    Material.SetFloat("_Intensity", Intensity);
            }
        }

        public int StepCount
        {
            get { return _stepCount; }
            set
            {
                _stepCount = value;
                if (Material)
                    Material.SetInt("_StepCount", _stepCount);
            }
        }

        protected virtual void Start()
        {
            Material = new Material(Shader);
            GetComponent<MeshFilter>().sharedMesh = Build();
            GetComponent<MeshRenderer>().sharedMaterial = Material;

            Material.SetFloat("_Intensity", Intensity);
            Material.SetFloat("_Opacity", Opacity);
            Material.SetInt("_StepCount", StepCount);
            Material.SetVector("_SliceMin", new Vector3(SliceXMin, SliceYMin, SliceZMin));
            Material.SetVector("_SliceMax", new Vector3(SliceXMax, SliceYMax, SliceZMax));
            Material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(_axis));
        }

        private void Update()
        {
            var correct = new Quaternion(AxisRoot.transform.localRotation.x , AxisRoot.transform.localRotation.y, AxisRoot.transform.localRotation.z, -AxisRoot.transform.localRotation.w);

            Material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(correct));
        }

        public void SliceMinMaxChanged()
        {
            if (!Material) return;
            Material.SetVector("_SliceMin", new Vector3(SliceXMin, SliceYMin, SliceZMin));
            Material.SetVector("_SliceMax", new Vector3(SliceXMax, SliceYMax, SliceZMax));
        }

        private static Mesh Build() {
            var vertices = new[] {
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

            var mesh = new Mesh {vertices = vertices, triangles = triangles};
            mesh.RecalculateNormals();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            return mesh;
        }

        private void OnValidate()
        {
            Constrain(ref SliceXMin, ref SliceXMax);
            Constrain(ref SliceYMin, ref SliceYMax);
            Constrain(ref SliceZMin, ref SliceZMax);

            if (!Material) return;

            Material.SetFloat("_Intensity", Intensity);
            Material.SetFloat("_Opacity", Opacity);
            Material.SetInt("_StepCount", StepCount);
            Material.SetVector("_SliceMin", new Vector3(SliceXMin, SliceYMin, SliceZMin));
            Material.SetVector("_SliceMax", new Vector3(SliceXMax, SliceYMax, SliceZMax));
            Material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(_axis));
        }

        private static void Constrain (ref float min, ref float max)
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
            Destroy(Material);
        }

        public void SetVolume(Texture3D texture3D)
        {
            Material.SetTexture("_Volume", texture3D);
        }
       
    }

}


