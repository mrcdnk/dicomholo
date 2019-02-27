using System;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
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

        public void SetCullAxisX()
        {
            _currentConfig = CullAxis.X;
            OnSelectCullAxis(_currentConfig);
        }

        public void SetCullAxisY()
        {
            _currentConfig = CullAxis.Y;
            OnSelectCullAxis(_currentConfig);

        }
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

        public void StepCountChanged(TubeSlider tubeSlider)
        {
            _volumeRendering.StepCount = tubeSlider.CurrentInt;
        }

        private void OnSelectCullAxis(CullAxis axis)
        {
            switch (axis)
            {
                case CullAxis.X:
                    _x.interactable = false;
                    _y.interactable = true;
                    _z.interactable = true;
                    _minSlider.CurrentFloat = _volumeRendering.sliceXMin;
                    _maxSlider.CurrentFloat = _volumeRendering.sliceXMax;

                    break;
                case CullAxis.Y:
                    _x.interactable = true;
                    _y.interactable = false;
                    _z.interactable = true;
                    _minSlider.CurrentFloat = _volumeRendering.sliceYMin;
                    _maxSlider.CurrentFloat = _volumeRendering.sliceYMax;
                    break;
                case CullAxis.Z:
                    _x.interactable = true;
                    _y.interactable = true;
                    _z.interactable = false;
                    _minSlider.CurrentFloat = _volumeRendering.sliceZMin;
                    _maxSlider.CurrentFloat = _volumeRendering.sliceZMax;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

        }

        public void OnMinChanged(TubeSlider tubeSlider)
        {
            switch (_currentConfig)
            {
                case CullAxis.X:
                    _volumeRendering.sliceXMin = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Y:
                    _volumeRendering.sliceYMin = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Z:
                    _volumeRendering.sliceZMin = (float) tubeSlider.CurrentPercentage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _volumeRendering.SliceMinMaxChanged();
        }

        public void OnMaxChanged(TubeSlider tubeSlider)
        {
            switch (_currentConfig)
            {
                case CullAxis.X:
                    _volumeRendering.sliceXMax = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Y:
                    _volumeRendering.sliceYMax = (float) tubeSlider.CurrentPercentage;
                    break;
                case CullAxis.Z:
                    _volumeRendering.sliceZMax = (float) tubeSlider.CurrentPercentage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _volumeRendering.SliceMinMaxChanged();
        }

        [Serializable]
        public enum CullAxis
        {
            X,
            Y,
            Z
        }
    }
}