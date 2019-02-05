using System;
using System.Collections.Generic;
using System.Linq;
using DICOMParser;
using ExtensionsMethods;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{

    public class Slice2DView : MonoBehaviour
    {
        private ImageStack _imageStack;
        public TubeSlider SliceSlider;
        public Button TransButton;
        public Button FrontButton;
        public Button SagButton;

        public RawImage Display;
        public RawImage SegmentImage;

        private PixelClickHandler[] _pixelClickHandlers;

        private readonly Dictionary<SliceType, int> _selection = new Dictionary<SliceType, int>();

        private readonly Dictionary<SliceType, Texture> _segmentTextures = new Dictionary<SliceType, Texture>(3);

        private SliceType _currentSliceType = SliceType.Transversal;

        public ImageStack ImageStack {
            set
            {
                _imageStack = value;

                Display.texture = value.GetTexture2D(_currentSliceType, _selection.GetValue(_currentSliceType));
            }
        }

        // Use this for initialization
        void Start()
        {
            Display = GetComponentInChildren<RawImage>();

            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                _selection[type] = 0;
            }

            SegmentImage.texture = new Texture2D(Display.texture.width, Display.texture.height);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Initialize()
        {
            if (_pixelClickHandlers == null)
            {
                _pixelClickHandlers = gameObject.GetComponentsInChildren<PixelClickHandler>();
            }

            SliceSlider.MaximumValue = _imageStack.GetMaxValue(_currentSliceType);
            SliceSlider.CurrentInt = _selection.GetValue(_currentSliceType, 0);

            foreach (var pixelClickHandler in _pixelClickHandlers)
            {
                pixelClickHandler.PixelClick.AddListener(OnPixelClicked);
            }
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
            SliceSlider.MaximumValue = _imageStack.GetMaxValue(_currentSliceType);
            SliceSlider.CurrentInt = _selection[_currentSliceType];
            Display.texture = _imageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            SegmentImage.texture = _segmentTextures[type];
        }

        public void SelectionChanged(TubeSlider slider)
        {
            if (_selection.Count == Enum.GetNames(typeof(SliceType)).Length && Display != null && _imageStack != null)
            {
                _selection[_currentSliceType] = slider.CurrentInt;
                Display.texture = _imageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            }
        }

        public void TextureUpdated(SliceType type, int index)
        {
            if (_currentSliceType == type && _selection[_currentSliceType] == index)
            {
                Display.texture = _imageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            }
        }

        private void OnPixelClicked(float x, float y)
        {
            Texture2D tex = SegmentImage.texture as Texture2D;

            int xCoord, yCoord;

            switch (_currentSliceType)
            {
                case SliceType.Transversal:
                    xCoord = Mathf.RoundToInt(x * _imageStack.Width);
                    yCoord = Mathf.RoundToInt(y * _imageStack.Height);
                    break;
                case SliceType.Sagittal:
                    xCoord = Mathf.RoundToInt(x * _imageStack.Height);
                    yCoord = Mathf.RoundToInt(y * _imageStack.Slices);
                    break;
                case SliceType.Frontal:     
                    xCoord = Mathf.RoundToInt(x * _imageStack.Width);
                    yCoord = Mathf.RoundToInt(y * _imageStack.Slices);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            tex.SetPixel(xCoord, yCoord, Color.red);
            tex.Apply();
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
