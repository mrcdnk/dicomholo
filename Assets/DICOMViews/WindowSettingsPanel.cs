using System;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    /// <summary>
    /// Can be used to manipulate the window Settings used for the textures or volumes
    /// </summary>
    public class WindowSettingsPanel : MonoBehaviour
    {
        public Toggle DefaultToggle;

        public TubeSlider WidthSlider;
        public TubeSlider CenterSlider;

        private double _windowWidth = Double.MinValue;

        public double WindowWidth
        {
            get { return DefaultToggle.isOn ? Double.MinValue : _windowWidth; }
        }

        private double _windowCenter = Double.MinValue;

        public double WindowCenter
        {
            get { return DefaultToggle.isOn ? Double.MinValue : _windowCenter; }
        }

        // Start is called before the first frame update
        void Start()
        {
            WidthSlider.SliderChangedEvent.AddListener(tubeSlider => _windowWidth = tubeSlider.CurrentDouble);
            CenterSlider.SliderChangedEvent.AddListener(tubeSlider => _windowCenter = tubeSlider.CurrentDouble);
        }

        // Update is called once per frame
        void Update()
        {
        }

        /// <summary>
        /// Use to configure the panel for the current DiFile
        /// </summary>
        /// <param name="min">minimum intensity value of the current DiFile</param>
        /// <param name="max">maximum intensity value of the current DiFile</param>
        public void Configure(int min, int max)
        {
            WidthSlider.MinimumValue = 0;
            WidthSlider.MaximumValue = max/2d;

            CenterSlider.MinimumValue = min;
            CenterSlider.MaximumValue = max;
        }
    }
}