using System;
using DICOMViews.Events;
using Segmentation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DICOMViews
{

    /// <summary>
    /// Configuration window for segments
    /// </summary>
    public class SegmentConfiguration : MonoBehaviour
    {
        public uint Display2Ds = 0xFFFFFFFF;
        public uint Display3Ds = 0xFFFFFFFF;
        public bool HideBase = false;

        public SegmentVisibilityChanged OnSelectionChanged2D = new SegmentVisibilityChanged();
        public SegmentVisibilityChanged OnSelectionChanged3D = new SegmentVisibilityChanged();
        public HideBaseChanged OnHideBaseChanged = new HideBaseChanged();

        [SerializeField] private Button[] _segmentButtons;

        [SerializeField] private Button _clear;
        [SerializeField] private Button _create;

        [SerializeField] private Image _selectedColor;

        [SerializeField] private Dropdown _segmentationStrategyChoice;

        [SerializeField] private GameObject _rangeParent;
        [SerializeField] private TubeSlider _minRange;
        [SerializeField] private TubeSlider _maxRange;

        [SerializeField] private GameObject _regionGrowParent;
        [SerializeField] private Text _seedXRegion;
        [SerializeField] private Text _seedYRegion;
        [SerializeField] private Text _seedZRegion;
        [SerializeField] private TubeSlider _thresholdRegion;

        [SerializeField] private Toggle _display2D;
        [SerializeField] private Toggle _display3D;
        [SerializeField] private Toggle _hideBaseData;

        private int _selectedSegment = 0;

        private SegmentCache _segmentCache;

        private bool _dontSendToggleEvent = false;

        private readonly RangeSegmentation.RangeParameter[] _rangeParameters =
            new RangeSegmentation.RangeParameter[SegmentCache.MaxSegmentCount];

        private readonly RegionGrowSegmentation.RegionGrowParameter[] _regionGrowParameters =
            new RegionGrowSegmentation.RegionGrowParameter[SegmentCache.MaxSegmentCount];

        private readonly RegionGrowSegmentation _regionGrowSegmentation = new RegionGrowSegmentation();
        private readonly RangeSegmentation _rangeSegmentation = new RangeSegmentation();

        private readonly SegmentationType[] _selectedType = new SegmentationType[SegmentCache.MaxSegmentCount];

        // Start is called before the first frame update
        private void Start()
        {
            _regionGrowParent.SetActive(false);
            _rangeParent.SetActive(true);

            for (var i = 0; i < SegmentCache.MaxSegmentCount; i++)
            {
                var ti = i;
                _segmentButtons[i].onClick.AddListener(() => ShowSeg(ti));
            }
            _segmentationStrategyChoice.onValueChanged.AddListener(SelectedType);

            _create.onClick.AddListener(CreateSelection);
            _clear.onClick.AddListener(() => _segmentCache.Clear(_selectedSegment));
            _display2D.onValueChanged.AddListener(Toggle2D);
            _display3D.onValueChanged.AddListener(Toggle3D);
            _hideBaseData.onValueChanged.AddListener(ToggleHideBase);
        }

        /// <summary>
        /// Initializes the configuration window for the given cache.
        /// </summary>
        /// <param name="cache">SegmentCache containing the segments</param>
        /// <param name="minIntensity">Minimum possible intensity in data</param>
        /// <param name="maxIntensity">Maximum possible intensity in data</param>
        public void Initialize(SegmentCache cache, int minIntensity, int maxIntensity)
        {
            _segmentCache = cache;

            _segmentCache.SegmentChanged.AddListener(delegate (uint segment)
            {
                ValidateCurrentParameters();
            });

            _selectedColor.color = cache.GetSegment(_selectedSegment).SegmentColor;
            _minRange.MinimumValue = minIntensity;
            _maxRange.MinimumValue = minIntensity;
            _minRange.MaximumValue = maxIntensity;
            _maxRange.MaximumValue = maxIntensity;
            _thresholdRegion.MinimumValue = 0;
            _thresholdRegion.MaximumValue = (maxIntensity - minIntensity) / 2f;

            for (var i = 0; i < SegmentCache.MaxSegmentCount; i++)
            {
                _selectedType[i] = SegmentationType.Range;
                _rangeParameters[i] = new RangeSegmentation.RangeParameter(minIntensity, maxIntensity, 2);
                _regionGrowParameters[i] = new RegionGrowSegmentation.RegionGrowParameter(-1, -1, -1);
            }

            UpdateRegionSeed(-1, -1, -1);
            ValidateCurrentParameters();
            _dontSendToggleEvent = true;
            UpdateToggles();
            _dontSendToggleEvent = false;
        }

        /// <summary>
        /// Checks if the current parameters are valid for segment creation and disables  or enables the creation button.
        /// </summary>
        private void ValidateCurrentParameters()
        {
            switch (_selectedType[_selectedSegment])
            {
                case SegmentationType.Range:
                    _create.interactable = _rangeParameters[_selectedSegment] != null &&
                                           _rangeParameters[_selectedSegment].IsValid();
                    break;
                case SegmentationType.RegionGrow:
                    _create.interactable = _regionGrowParameters[_selectedSegment] != null &&
                                           _regionGrowParameters[_selectedSegment].IsValid();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Creates a Segment with the current selection.
        /// </summary>
        private void CreateSelection()
        {
            _create.interactable = false;
            switch (_selectedType[_selectedSegment])
            {
                case SegmentationType.Range:
                    _segmentCache.CreateSegment(SegmentCache.GetSelector(_selectedSegment), _rangeSegmentation,
                        _rangeParameters[_selectedSegment], false);
                    break;
                case SegmentationType.RegionGrow:
                    _segmentCache.CreateSegment(SegmentCache.GetSelector(_selectedSegment), _regionGrowSegmentation,
                        _regionGrowParameters[_selectedSegment]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Toggles if the selected segment is visible in 2D or not.
        /// </summary>
        /// <param name="b">new toggle value</param>
        private void Toggle2D(bool b)
        {
            if(_dontSendToggleEvent) return;
            Display2Ds = SegmentCache.ToggleIndex(Display2Ds, _selectedSegment);
            OnSelectionChanged2D.Invoke(Display2Ds);
        }

        /// <summary>
        /// Toggles if the selected segment is visible in 3D or not.
        /// </summary>
        /// <param name="b">new toggle value</param>
        private void Toggle3D(bool b)
        {
            if (_dontSendToggleEvent) return;
            Display3Ds = SegmentCache.ToggleIndex(Display3Ds, _selectedSegment);
            OnSelectionChanged3D.Invoke(Display3Ds);
        }

        /// <summary>
        /// Toggles if the base data is visible in 3D or not.
        /// </summary>
        /// <param name="b">new toggle value</param>
        private void ToggleHideBase(bool b)
        {
            HideBase = b;
            OnHideBaseChanged.Invoke(b);
        }

        /// <summary>
        /// Updates the 2D and 3D toggle for the current selection.
        /// </summary>
        private void UpdateToggles()
        {
            _display2D.isOn = SegmentCache.ContainsIndex(Display2Ds, _selectedSegment);
            _display3D.isOn = SegmentCache.ContainsIndex(Display3Ds, _selectedSegment);
        }

        /// <summary>
        /// Updates the selected seed for region grow segmentation
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void UpdateRegionSeed(int x, int y, int z)
        {
            _regionGrowParameters[_selectedSegment].X = x;
            _regionGrowParameters[_selectedSegment].Y = y;
            _regionGrowParameters[_selectedSegment].Z = z;

            _seedXRegion.text = "x: " + x;
            _seedYRegion.text = "y: " + y;
            _seedZRegion.text = "z: " + z;

            ValidateCurrentParameters();
        }

        /// <summary>
        /// Listener for the threshold slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void UpdateThreshold(TubeSlider tubeSlider)
        {
            if (_regionGrowParameters[_selectedSegment] != null)
            {
                _regionGrowParameters[_selectedSegment].Threshold = tubeSlider.CurrentInt;
            }
        }

        /// <summary>
        /// Listener for the region min slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void UpdateRegionMin(TubeSlider tubeSlider)
        {
            if (_rangeParameters[_selectedSegment] != null)
            {
                _rangeParameters[_selectedSegment].Lower = tubeSlider.CurrentInt;
            }
        }

        /// <summary>
        /// Listener for the region max slider
        /// </summary>
        /// <param name="tubeSlider"></param>
        public void UpdateRegionMax(TubeSlider tubeSlider)
        {
            if (_rangeParameters[_selectedSegment] != null)
            {
                _rangeParameters[_selectedSegment].Upper = tubeSlider.CurrentInt;
            }
        }

        /// <summary>
        /// Switches the currently selected segment
        /// </summary>
        /// <param name="index">Index of the segment in the segment cache</param>
        private void ShowSeg(int index)
        {
            for (var i = 0; i < SegmentCache.MaxSegmentCount; i++)
            {
                _segmentButtons[i].interactable = i != index;
            }

            _selectedSegment = index;
            _selectedColor.color = _segmentCache.GetSegment(_selectedSegment).SegmentColor;
            ValidateCurrentParameters();
            _dontSendToggleEvent = true;
            UpdateToggles();
            _dontSendToggleEvent = false;
        }

        /// <summary>
        /// Listener for selection of Segmentation type
        /// </summary>
        /// <param name="index"></param>
        private void SelectedType(int index)
        {
            switch (_segmentationStrategyChoice.captionText.text)
            {
                case "Range":
                    _regionGrowParent.SetActive(false);
                    _rangeParent.SetActive(true);
                    _selectedType[_selectedSegment] = SegmentationType.Range;
                    break;
                case "Region Grow":
                    _regionGrowParent.SetActive(true);
                    _rangeParent.SetActive(false);
                    _selectedType[_selectedSegment] = SegmentationType.RegionGrow;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ValidateCurrentParameters();
        }

        /// <summary>
        /// Possible Segmentation Types
        /// </summary>
        private enum SegmentationType
        {
            Range,
            RegionGrow
        }
    }
}
