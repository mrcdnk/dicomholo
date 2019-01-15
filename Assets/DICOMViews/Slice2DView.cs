using System;
using System.Collections.Generic;
using System.Linq;
using DICOMParser;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{

    public class Slice2DView : MonoBehaviour
    {
        public ImageStack ImageStack;
        public TubeSlider SliceSlider;
        public Button TransButton;
        public Button FrontButton;
        public Button SagButton;

        private RawImage _display;

        private readonly Dictionary<SliceType, int> _selection = new Dictionary<SliceType, int>();

        private SliceType _currentSliceType = SliceType.Transversal;

        // Use this for initialization
        void Start()
        {
            _display = GetComponentInChildren<RawImage>();

            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                _selection[type] = 0;
            }

            if (ImageStack != null && ImageStack.HasData(_currentSliceType))
            {
                _display.texture = ImageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void InitSlider()
        {
            SliceSlider.MaximumValue = ImageStack.GetMaxValue(_currentSliceType);
            SliceSlider.CurrentInt = _selection[_currentSliceType];
        }

        public void ShowTrans()
        {
            TransButton.interactable = false;
            FrontButton.interactable = true;
            SagButton.interactable = true;
            Show(SliceType.Transversal);
        }

        public void ShowFront()
        {
            TransButton.interactable = true;
            FrontButton.interactable = false;
            SagButton.interactable = true;
            Show(SliceType.Frontal);
        }

        public void ShowSag()
        {
            TransButton.interactable = true;
            FrontButton.interactable = true;
            SagButton.interactable = false;
            Show(SliceType.Sagittal);
        }

        public void Show(SliceType type)
        {
            _currentSliceType = type;
            SliceSlider.MaximumValue = ImageStack.GetMaxValue(_currentSliceType);
            SliceSlider.CurrentInt = _selection[_currentSliceType];
            _display.texture = ImageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
        }

        public void SelectionChanged(TubeSlider slider)
        {
            if (_selection != null && _display != null)
            {
                _selection[_currentSliceType] = slider.CurrentInt;
                _display.texture = ImageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            }
        }

        public void TextureUpdated(SliceType type, int index)
        {
            if (_currentSliceType == type && _selection[_currentSliceType] == index)
            {
                _display.texture = ImageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            }
        }

        public SliceType GetCurrentSliceType()
        {
            return _currentSliceType;
        }

        public int GetSelection(SliceType type)
        {
            return _selection[type];
        }

    }
}
