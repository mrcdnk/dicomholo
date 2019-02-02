using UnityEngine;

namespace DICOMViews
{
    public class VolumeSettingsPanel : MonoBehaviour
    {
        [SerializeField] private VolumeRendering.VolumeRendering _volumeRendering;

        [SerializeField] private TubeSlider _intensitySlider;
        [SerializeField] private TubeSlider _opacitySlider;


        // Start is called before the first frame update
        void Start()
        {
            _intensitySlider.CurrentFloat = _volumeRendering.Intensity;
            _opacitySlider.CurrentFloat = _volumeRendering.Opacity;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void IntensityChanged(TubeSlider tubeSlider)
        {
            _volumeRendering.Intensity = tubeSlider.CurrentFloat;
        }

        public void OpacityChanged(TubeSlider tubeSlider)
        {
            _volumeRendering.Opacity = tubeSlider.CurrentFloat;
        }
    }
}
