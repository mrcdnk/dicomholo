using System;
using System.Collections.Generic;
using System.Linq;
using DICOMParser;
using DICOMViews.Events;
using ExtensionsMethods;
using Segmentation;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    /// <summary>
    /// 2D View of the slices in the ImageStack
    /// </summary>
    public class Slice2DView : MonoBehaviour
    {
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
        public SliceType CurrentSliceType { get; private set; } = SliceType.Transversal;

        private ImageStack _imageStack;
        private PixelClickHandler _pixelClickHandler;

        private int _lastClickX = -1;
        private int _lastClickY = -1;

        private bool _hasBeenClicked = false;

        [SerializeField] private Color32 _segmentTransparency = new Color32(255, 255, 255, 75);

        private readonly Dictionary<SliceType, int> _selection = new Dictionary<SliceType, int>();

#if PRINT_USAGE
        private int _numberClicks = 0;
#endif

        // Use this for initialization
        private void Start()
        {
            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                _selection[type] = 0;
            }
        }

        /// <summary>
        /// Initializes the 2D view with the given ImageStack
        /// </summary>
        public void Initialize(ImageStack imageStack)
        {
            if (_pixelClickHandler == null)
            {
                _pixelClickHandler = gameObject.GetComponentInChildren<PixelClickHandler>();
            }

            _imageStack = imageStack;

            SliceSlider.MaximumValue = _imageStack.GetMaxValue(CurrentSliceType);
            SliceSlider.CurrentInt = _selection.GetValue(CurrentSliceType, 0);

            _pixelClickHandler.PixelClick.AddListener(OnPixelClicked);

            try
            {
                // key might not be present in dictionary
                Display.texture = _imageStack.GetTexture2D(CurrentSliceType, _selection.GetValue(CurrentSliceType));
            }
            finally
            {
                ResetClickDisplay(_imageStack.Width, _imageStack.Height);
                ClickDisplay.color = new Color32(255, 255, 255, 255);
            }         
        }

        /// <summary>
        /// Resets and resizes the ClickDisplay.
        /// </summary>
        /// <param name="width">new width of the click display</param>
        /// <param name="height">new height of the click display</param>
        private void ResetClickDisplay(int width, int height)
        {
            var other = new Texture2D(width, height, TextureFormat.ARGB32, false);
            other.SetPixels32(new Color32[width * height]);
            ClickDisplay.texture = other;
            other.Apply();
        }

        /// <summary>
        /// Show Transversal textures.
        /// </summary>
        public void ShowTrans()
        {
            TransButton.interactable = false;
            FrontButton.interactable = true;
            SagButton.interactable = true;
            Show(SliceType.Transversal);
        }

        /// <summary>
        /// Show Frontal textures.
        /// </summary>
        public void ShowFront()
        {
            TransButton.interactable = true;
            FrontButton.interactable = false;
            SagButton.interactable = true;
            Show(SliceType.Frontal);
        }

        /// <summary>
        /// Show Saggital textures.
        /// </summary>
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
            CurrentSliceType = type;
            SliceSlider.MaximumValue = _imageStack.GetMaxValue(CurrentSliceType);
            SliceSlider.CurrentInt = _selection[CurrentSliceType];

            Display.texture = _imageStack.GetTexture2D(CurrentSliceType, _selection[CurrentSliceType]);

            if (ClickDisplay.texture.width != Display.texture.width ||
                ClickDisplay.texture.height != Display.texture.height)
            {
                ResetClickDisplay(Display.texture.width, Display.texture.height);
            }
            else
            {
               ResetClick();
            }
        }

        /// <summary>
        /// Handles the change of the currently selected slice
        /// </summary>
        /// <param name="slider">Slider that was changed</param>
        public void SelectionChanged(TubeSlider slider)
        {
            if (_selection.Count == Enum.GetNames(typeof(SliceType)).Length && Display != null && _imageStack != null)
            {
                _selection[CurrentSliceType] = slider.CurrentInt;

                Display.texture = _imageStack.GetTexture2D(CurrentSliceType, _selection[CurrentSliceType]);
                SegmentImage.texture = SegmentCache.GetSegmentTexture(CurrentSliceType, _selection[CurrentSliceType]);

                ResetClick();
            }
        }

        /// <summary>
        /// Handles texture update events.
        /// </summary>
        /// <param name="type">type of texture that was updated</param>
        /// <param name="index">index of the updated texture</param>
        public void TextureUpdated(SliceType type, int index)
        {
            if (CurrentSliceType == type && _selection[CurrentSliceType] == index)
            {
                Display.texture = _imageStack.GetTexture2D(CurrentSliceType, _selection[CurrentSliceType]);

                ResetClick();
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
            if (CurrentSliceType != type || !_selection.ContainsKey(CurrentSliceType) || _selection[CurrentSliceType] != index)
            {
                return;
            }

            SegmentImage.texture = tex;
            SegmentImage.color = _segmentTransparency;
        }

        /// <summary>
        /// Handles a click on the display texture
        /// </summary>
        /// <param name="x">percentage of the width</param>
        /// <param name="y">percentage of the height</param>
        private void OnPixelClicked(float x, float y)
        {
            int xCoord, yCoord;

#if PRINT_USAGE
            _numberClicks++;
            Debug.Log(Time.time +
                      $" : Click on 2D View number {_numberClicks})");
#endif

            switch (CurrentSliceType)
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

            SetClick(xCoord, yCoord);          

            InvokePointSelected();
        }

        /// <summary>
        /// Sets the selection of the clickDisplay
        /// </summary>
        /// <param name="x">Selected x</param>
        /// <param name="y">Selected y</param>
        private void SetClick(int x, int y)
        {
            var tex = ClickDisplay.texture as Texture2D;

            if (!tex)
            {
                return;
            }

            if (_hasBeenClicked && _lastClickX > -1 && _lastClickY > -1)
            {
                tex.SetPixel(_lastClickX, _lastClickY, Color.clear);
            }

            tex.SetPixel(x, y, SelectionColor);
            tex.Apply();

            _lastClickX = x;
            _lastClickY = y;
            _hasBeenClicked = true;
        }

        /// <summary>
        /// Removes last click, if there was one.
        /// </summary>
        private void ResetClick()
        {  
            var tex = ClickDisplay.texture as Texture2D;

            if (tex)
            {
                tex.SetPixel(_lastClickX, _lastClickY, Color.clear);        
                tex.Apply();
            }

            _lastClickX = -1;
            _lastClickY = -1;
            _hasBeenClicked = false;
            InvokePointSelected();
        }

        /// <summary>
        /// Invokes the point selected event for the currently selected pixel
        /// </summary>
        private void InvokePointSelected()
        {
            var selection = -1;

            _selection.TryGetValue(CurrentSliceType, out selection);

            switch (CurrentSliceType)
            {
                case SliceType.Transversal:
                    OnPointSelected.Invoke(_lastClickX, _lastClickY, selection);
                    break;
                case SliceType.Sagittal:
                    OnPointSelected.Invoke(selection, _lastClickX, _lastClickY);
                    break;
                case SliceType.Frontal:
                    OnPointSelected.Invoke(_lastClickX, selection, _lastClickY);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns the selected slice for the given SliceType
        /// </summary>
        /// <param name="type">Type of slice to get selection of</param>
        /// <returns>index of the selected slice</returns>
        public int GetSelection(SliceType type)
        {
            return _selection[type];
        }

    }
}
