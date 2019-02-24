using System;
using System.Collections.Generic;
using System.Globalization;
using DICOMParser;
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
        public Dropdown DefaultSettings;

        public TubeSlider WidthSlider;
        public TubeSlider CenterSlider;

        public Button LoadTextures;
        public Button LoadVolume;

        public WindowSettingsChanged SettingsChangedEvent = new WindowSettingsChanged();

        public double WindowWidth { get; private set; } = double.MinValue;

        public double WindowCenter { get; private set; } = double.MinValue;


        private Dictionary<string, Tuple<double, double>> _presets = new Dictionary<string, Tuple<double, double>>();

        // Start is called before the first frame update
        private void Start()
        {
            WidthSlider.SliderChangedEvent.AddListener(delegate(TubeSlider tubeSlider)
            {
                WindowWidth = tubeSlider.CurrentInt;
                SettingsChangedEvent.Invoke(WindowWidth, WindowCenter);          
            });
            CenterSlider.SliderChangedEvent.AddListener(delegate(TubeSlider tubeSlider)
            {
                WindowCenter = tubeSlider.CurrentInt;
                SettingsChangedEvent.Invoke(WindowWidth, WindowCenter);
            });
            DefaultSettings.onValueChanged.AddListener(delegate
            {
                var selection = _presets[DefaultSettings.captionText.text];
                WindowWidth = selection.Item1;
                WindowCenter = selection.Item2;

                WidthSlider.CurrentDouble = WindowWidth;
                CenterSlider.CurrentDouble = WindowCenter;

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
        public void Configure(int min, int max, double[] currentWidth , double[] currentCenter)
        {
            WidthSlider.MinimumValue = 1;
            WidthSlider.MaximumValue = max/2d;

            if (currentWidth[0] > double.MinValue)
            {
                WidthSlider.CurrentDouble = currentWidth[0];
            }
            else
            {
                WidthSlider.CurrentDouble = WidthSlider.MaximumValue;
            }


            CenterSlider.MinimumValue = min;
            CenterSlider.MaximumValue = max;
            if (currentCenter[0] > double.MinValue)
            {
                CenterSlider.CurrentDouble = currentCenter[0];
            }
            else
            {
                CenterSlider.CurrentDouble = CenterSlider.MinimumValue + WidthSlider.CurrentDouble;
            }

            WindowWidth = WidthSlider.CurrentDouble;
            WindowCenter = CenterSlider.CurrentDouble;

            if (currentWidth[0] > double.MinValue && currentCenter[0] > double.MinValue)
            {
                RegisterDefaultSettings(currentWidth, currentCenter);
            }
            else
            {
                RegisterDefaultSettings(new[]{WindowWidth}, new[]{WindowCenter});
            }
        }

        /// <summary>
        /// Creates a new preset dictionary
        /// </summary>
        /// <param name="widthPresets">array of preset widths</param>
        /// <param name="centerPresets">array of preset heights</param>
        private void RegisterDefaultSettings(IReadOnlyList<double> widthPresets, IReadOnlyList<double> centerPresets)
        {
            _presets.Clear();

            for (var i = 0; i < widthPresets.Count; i++)
            {
                _presets.Add("W"+Convert.ToDecimal($"{Convert.ToDecimal(widthPresets[i]):0.00}").ToString(CultureInfo.InvariantCulture)+
                             "C"+ Convert.ToDecimal($"{Convert.ToDecimal(centerPresets[i]):0.00}").ToString(CultureInfo.InvariantCulture), 
                                new Tuple<double, double>(widthPresets[i], centerPresets[i]));
            }

            DefaultSettings.AddOptions(new List<string>(_presets.Keys));
        }

        /// <summary>
        /// Disables buttons of Settings Panel.
        /// </summary>
        public void DisableButtons()
        {
            LoadVolume.enabled = false;
            LoadTextures.enabled = false;
            
        }

        /// <summary>
        /// Enables buttons of Settings Panel.
        /// </summary>
        public void EnableButtons()
        {
            LoadVolume.enabled = true;
            LoadTextures.enabled = true;
        }
    }
}