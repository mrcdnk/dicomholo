using System;
using DICOMViews.Events;
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

        public Button LoadTextures;
        public Button LoadVolume;

        public WindowSettingsChanged SettingsChangedEvent = new WindowSettingsChanged();
        private double _windowWidth = double.MinValue;

        public double WindowWidth => DefaultToggle.isOn ? double.MinValue : _windowWidth;

        private double _windowCenter = double.MinValue;

        public double WindowCenter => DefaultToggle.isOn ? double.MinValue : _windowCenter;

        // Start is called before the first frame update
        void Start()
        {
            WidthSlider.SliderChangedEvent.AddListener(delegate(TubeSlider tubeSlider)
            {
                _windowWidth = tubeSlider.CurrentDouble;
                SettingsChangedEvent.Invoke(WindowWidth, WindowCenter);
            });
            CenterSlider.SliderChangedEvent.AddListener(delegate(TubeSlider tubeSlider)
            {
                _windowCenter = tubeSlider.CurrentDouble;
                SettingsChangedEvent.Invoke(WindowWidth, WindowCenter);
            });
            DefaultToggle.onValueChanged.AddListener(delegate
            {
                SettingsChangedEvent.Invoke(WindowWidth, WindowCenter);
            });
        }

        /// <summary>
        /// Use to configure the panel for the current DiFile
        /// </summary>
        /// <param name="min">minimum intensity value of the current DiFile</param>
        /// <param name="max">maximum intensity value of the current DiFile</param>
        /// <param name="currentWidth">window width read from the DiFile</param>
        /// <param name="currentCenter">window center read from the DiFile</param>
        public void Configure(int min, int max, double currentWidth = double.MinValue, double currentCenter = double.MinValue)
        {
            WidthSlider.MinimumValue = 1;
            WidthSlider.MaximumValue = max/2d;
            if (currentWidth > double.MinValue)
            {
                WidthSlider.CurrentDouble = currentWidth;
            }
            else
            {
                WidthSlider.CurrentDouble = WidthSlider.MaximumValue;
            }


            CenterSlider.MinimumValue = min;
            CenterSlider.MaximumValue = max;
            if (currentCenter > double.MinValue)
            {
                CenterSlider.CurrentDouble = currentCenter;
            }
            else
            {
                CenterSlider.CurrentDouble = CenterSlider.MinimumValue + WidthSlider.CurrentDouble;
            }

            DefaultToggle.isOn = true;
        }

        public void DisableButtons()
        {
            LoadVolume.enabled = false;
            LoadTextures.enabled = false;
        }

        public void EnableButtons()
        {
            LoadVolume.enabled = true;
            LoadTextures.enabled = true;
        }
    }
}