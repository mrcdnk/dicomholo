using System;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    /// <summary>
    /// Panel for configuring the volume rendering object
    /// </summary>
    public class VolumeSettingsPanel : MonoBehaviour
    {
        [SerializeField] private VolumeRendering.VolumeRendering _volumeRendering = null;

        [SerializeField] private TubeSlider _intensitySlider = null;
        [SerializeField] private TubeSlider _opacitySlider = null;
        [SerializeField] private TubeSlider _stepCountSlider = null;

        [SerializeField] private GameObject _rotationObject = null;

        [SerializeField] private TubeSlider _maxSlider = null;
        [SerializeField] private TubeSlider _minSlider = null;

        [SerializeField] private Button _x = null;
        [SerializeField] private Button _y = null;
        [SerializeField] private Button _z = null;

        private CullAxis _currentConfig = CullAxis.X;

#if PRINT_USAGE
        private void OnApplicationQuit()
        {
            Debug.Log(Time.time +
                      $" : Ended with Settings (Intensity: {_intensitySlider.CurrentDouble:0.00}," +
                      $" Opacity: {_opacitySlider.CurrentDouble:0.00}, " +
                      $"StepCount: {_stepCountSlider.CurrentInt}, " +
                      $"xMin: {_volumeRendering.SliceXMin}, " +
                      $"xMax: {_volumeRendering.SliceXMax}, " +
                      $"yMin: {_volumeRendering.SliceZMin}, " +
                      $"yMax: {_volumeRendering.SliceZMax}, " +
                      $"zMin: {_volumeRendering.SliceZMin}, " +
                      $"zMax: {_volumeRendering.SliceZMax}) ");
        }  
#endif

        /// <summary>
        /// Changes the currently selected cull axis to the X Axis
        /// </summary>
        public void SetCullAxisX()
        {
            _currentConfig = CullAxis.X;
            OnSelectCullAxis(_currentConfig);
        }


        /// <summary>
        /// Changes the currently selected cull axis to the Y Axis
        /// </summary>
        public void SetCullAxisY()
        {
            _currentConfig = CullAxis.Y;
            OnSelectCullAxis(_currentConfig);

        }


        /// <summary>
        /// Changes the currently selected cull axis to the Z Axis
        /// </summary>
        public void SetCullAxisZ()
        {
            _currentConfig = CullAxis.Z;
            OnSelectCullAxis(_currentConfig);

        }

        // Start is called before the first frame update
        protected void Start()
        {
            gameObject.SetActive(false);
            _intensitySlider.CurrentFloat = _volumeRendering.Intensity;
            _opacitySlider.CurrentFloat = _volumeRendering.Opacity;
            _stepCountSlider.CurrentInt = _volumeRendering.StepCount;
        }

        /// <summary>
        /// Toggles the active state of this gameObject
        /// </summary>
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        /// <summary>
        /// Listener for intensity slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void IntensityChanged(TubeSlider tubeSlider)
        {
            _volumeRendering.Intensity = tubeSlider.CurrentFloat;
        }

        /// <summary>
        /// Listener for opacity slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void OpacityChanged(TubeSlider tubeSlider)
        {
            _volumeRendering.Opacity = tubeSlider.CurrentFloat;
        }

        /// <summary>
        /// Listener for step count slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void StepCountChanged(TubeSlider tubeSlider)
        {
            _volumeRendering.StepCount = tubeSlider.CurrentInt;
        }

        /// <summary>
        /// Updates the UI to represent the given axis
        /// </summary>
        /// <param name="axis">CullAxis to display</param>
        private void OnSelectCullAxis(CullAxis axis)
        {
            switch (axis)
            {
                case CullAxis.X:
                    _x.interactable = false;
                    _y.interactable = true;
                    _z.interactable = true;
                    _minSlider.CurrentFloat = _volumeRendering.SliceXMin;
                    _maxSlider.CurrentFloat = _volumeRendering.SliceXMax;

                    break;
                case CullAxis.Y:
                    _x.interactable = true;
                    _y.interactable = false;
                    _z.interactable = true;
                    _minSlider.CurrentFloat = _volumeRendering.SliceYMin;
                    _maxSlider.CurrentFloat = _volumeRendering.SliceYMax;
                    break;
                case CullAxis.Z:
                    _x.interactable = true;
                    _y.interactable = true;
                    _z.interactable = false;
                    _minSlider.CurrentFloat = _volumeRendering.SliceZMin;
                    _maxSlider.CurrentFloat = _volumeRendering.SliceZMax;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

        }

        /// <summary>
        /// Listener for the min slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void OnMinChanged(TubeSlider tubeSlider)
        {
            switch (_currentConfig)
            {
                case CullAxis.X:
                    _volumeRendering.SliceXMin = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Y:
                    _volumeRendering.SliceYMin = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Z:
                    _volumeRendering.SliceZMin = (float) tubeSlider.CurrentPercentage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _volumeRendering.SliceMinMaxChanged();
        }

        /// <summary>
        /// Listener for the max slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void OnMaxChanged(TubeSlider tubeSlider)
        {
            switch (_currentConfig)
            {
                case CullAxis.X:
                    _volumeRendering.SliceXMax = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Y:
                    _volumeRendering.SliceYMax = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Z:
                    _volumeRendering.SliceZMax = (float) tubeSlider.CurrentPercentage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _volumeRendering.SliceMinMaxChanged();
        }

        /// <summary>
        /// Defines an Axis that can be culled.
        /// </summary>
        [Serializable]
        public enum CullAxis
        {
            X,
            Y,
            Z
        }
    }
}