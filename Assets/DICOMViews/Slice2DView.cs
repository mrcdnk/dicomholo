using System;
using System.Collections.Generic;
using System.Linq;
using DICOMParser;
using ExtensionsMethods;
using Segmentation;
using Threads;
using UnityEngine;
using UnityEngine.Events;
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
        public RawImage ClickDisplay;

        public SegmentCache SegmentCache;
        public PointSelected OnPointSelected = new PointSelected();
        
        public Color SelectionColor = Color.yellow;

        private PixelClickHandler _pixelClickHandler;

        private readonly Dictionary<SliceType, int> _selection = new Dictionary<SliceType, int>();

        private SliceType _currentSliceType = SliceType.Transversal;

        private int lastClickX = -1;
        private int lastClickY = -1;

        private bool hasBeenClicked = false;

        [SerializeField] private Color32 segmentTransparency = new Color32(255, 255, 255, 70);

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
            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                _selection[type] = 0;
            }
        }

        // Update is called once per frame
        void Update()
        {
          
        }

        public void Initialize()
        {
            if (_pixelClickHandler == null)
            {
                _pixelClickHandler = gameObject.GetComponentInChildren<PixelClickHandler>();
            }

            SliceSlider.MaximumValue = _imageStack.GetMaxValue(_currentSliceType);
            SliceSlider.CurrentInt = _selection.GetValue(_currentSliceType, 0);

            _pixelClickHandler.PixelClick.AddListener(OnPixelClicked); 
    
            ResetClickDisplay(_imageStack.Width, _imageStack.Height);
            ClickDisplay.color = new Color32(255, 255, 255, 255);
        }

        private void ResetClickDisplay(int width, int height)
        {
            Texture2D other = new Texture2D(width, height, TextureFormat.ARGB32, false);

            other.SetPixels32(new Color32[width * height]);
            ClickDisplay.texture = other;
            other.Apply();
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

        /// <summary>
        /// Swaps the displayed SliceType for the given SliceType
        /// </summary>
        /// <param name="type">SliceType to display.</param>
        public void Show(SliceType type)
        {
            _currentSliceType = type;
            SliceSlider.MaximumValue = _imageStack.GetMaxValue(_currentSliceType);
            SliceSlider.CurrentInt = _selection[_currentSliceType];
            Display.texture = _imageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);

            if (ClickDisplay.texture.width != Display.texture.width ||
                ClickDisplay.texture.height != Display.texture.height)
            {
                ResetClickDisplay(Display.texture.width, Display.texture.height);
            }
            else
            {
                Texture2D tex = ClickDisplay.texture as Texture2D;
                tex.SetPixel(lastClickX, lastClickY, Color.clear);
                tex.Apply();
            }

            lastClickY = -1;
            lastClickX = -1;
            hasBeenClicked = false;
        }

        /// <summary>
        /// Handles the change of the currently selected slice
        /// </summary>
        /// <param name="slider">Slider that was changed</param>
        public void SelectionChanged(TubeSlider slider)
        {
            if (_selection.Count == Enum.GetNames(typeof(SliceType)).Length && Display != null && _imageStack != null)
            {
                _selection[_currentSliceType] = slider.CurrentInt;
                Display.texture = _imageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
                SegmentImage.texture = SegmentCache.GetSegmentTexture(_currentSliceType, _selection[_currentSliceType]);

                Texture2D tex = ClickDisplay.texture as Texture2D;

                tex.SetPixel(lastClickX, lastClickY, Color.clear);
                tex.Apply();

                lastClickX = -1;
                lastClickY = -1;
                hasBeenClicked = false;
            }
        }

        /// <summary>
        /// Handles texture update events.
        /// </summary>
        /// <param name="type">type of texture that was updated</param>
        /// <param name="index">index of the updated texture</param>
        public void TextureUpdated(SliceType type, int index)
        {
            if (_currentSliceType == type && _selection[_currentSliceType] == index)
            {
                Display.texture = _imageStack.GetTexture2D(_currentSliceType, _selection[_currentSliceType]);
            }
        }

        /// <summary>
        /// Handles Segment update events
        /// </summary>
        /// <param name="tex">actual texture that was updated</param>
        /// <param name="type">SliceType of the texture</param>
        /// <param name="index">index of the updated texture</param>
        public void SegmentTextureUpdated(Texture2D tex, SliceType type, int index)
        {
            if (_currentSliceType != type || !_selection.ContainsKey(_currentSliceType) || _selection[_currentSliceType] != index)
            {
                return;
            }

            SegmentImage.texture = tex;
            SegmentImage.color = segmentTransparency;
        }

        /// <summary>
        /// Handles a click on the display texture
        /// </summary>
        /// <param name="x">percentage of the width</param>
        /// <param name="y">percentage of the height</param>
        private void OnPixelClicked(float x, float y)
        {
            Texture2D tex = ClickDisplay.texture as Texture2D;

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

            if (hasBeenClicked && lastClickX > -1 && lastClickY > -1)
            {
                tex.SetPixel(lastClickX, lastClickY, Color.clear);
            }

            lastClickX = xCoord;
            lastClickY = yCoord;
            hasBeenClicked = true;

            tex.SetPixel(xCoord, yCoord, SelectionColor);
            tex.Apply();

            InvokePointSelected();
        }

        private void InvokePointSelected()
        {
            switch (_currentSliceType)
            {
                case SliceType.Transversal:
                    OnPointSelected.Invoke(lastClickX, lastClickY, _selection[_currentSliceType]);
                    break;
                case SliceType.Sagittal:
                    OnPointSelected.Invoke(_selection[_currentSliceType], lastClickX, lastClickY);
                    break;
                case SliceType.Frontal:
                    OnPointSelected.Invoke(lastClickX, _selection[_currentSliceType], lastClickY);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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


        public class PointSelected : UnityEvent<int, int, int>
        {

        }
    }
}
