using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    public class WindowSettingsPanel : MonoBehaviour
    {
        public Toggle DefaultToggle;

        public TubeSlider WidthSlider;
        public TubeSlider CenterSlider;

        private int _valueRange;

        private int _windowWidth = -1;

        public int WindowWidth
        {
            get { return DefaultToggle.isOn ? -1 : _windowWidth; }
        }

        private int _windowCenter = -1;

        public int WindowCenter
        {
            get { return DefaultToggle.isOn ? -1 : _windowCenter; }
        }

        // Start is called before the first frame update
        void Start()
        {
            WidthSlider.SliderChangedEvent.AddListener(TubeSlider => _windowWidth = TubeSlider.CurrentInt);
            CenterSlider.SliderChangedEvent.AddListener(TubeSlider => _windowCenter = TubeSlider.CurrentInt);
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void Configure(int min, int max)
        {
            _valueRange = max - min;

            WidthSlider.MinimumValue = 0;
            WidthSlider.MaximumValue = max/2;

            CenterSlider.MinimumValue = min;
            CenterSlider.MaximumValue = max;
        }
    }
}