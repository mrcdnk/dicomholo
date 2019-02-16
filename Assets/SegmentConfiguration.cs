﻿using System;
using System.Collections;
using System.Collections.Generic;
using Segmentation;
using UnityEngine;
using UnityEngine.UI;

public class SegmentConfiguration : MonoBehaviour
{
    [SerializeField] private Button[] _segmentButtons;
    private RangeSegmentation.RangeParameter[] _rangeParameters = new RangeSegmentation.RangeParameter[SegmentCache.MaxSegmentCount];
    private RegionFillSegmentation.RegionFillParameter[] _regionFillParameters = new RegionFillSegmentation.RegionFillParameter[SegmentCache.MaxSegmentCount];

    [SerializeField] private Button _clear;
    [SerializeField] private Button _create;

    [SerializeField] private Image _selectedColor;

    [SerializeField] private Dropdown _segmentationStrategyChoice;

    [SerializeField] private GameObject _rangeParent;
    [SerializeField] private TubeSlider _minRange;
    [SerializeField] private TubeSlider _maxRange;

    [SerializeField] private GameObject _regionFillParent;
    [SerializeField] private Text _seedXRegion;
    [SerializeField] private Text _seedYRegion;
    [SerializeField] private Text _seedZRegion;
    [SerializeField] private TubeSlider _thresholdRegion;

    private readonly RegionFillSegmentation _regionFillSegmentation = new RegionFillSegmentation();
    private readonly RangeSegmentation _rangeSegmentation = new RangeSegmentation();

    private int _selectedSegment = 0;
    private readonly SegmentationType[] _selectedType = new SegmentationType[SegmentCache.MaxSegmentCount];

    private SegmentCache _segmentCache;

    // Start is called before the first frame update
    void Start()
    {
        _regionFillParent.SetActive(false);
        _rangeParent.SetActive(true);

        for (int i = 0 ; i< SegmentCache.MaxSegmentCount; i++)
        {
            int ti = i; 
            _segmentButtons[i].onClick.AddListener(() => ShowSeg(ti));
        }

        _segmentationStrategyChoice.onValueChanged.AddListener(SelectedType);

        _create.onClick.AddListener(CreateSelection);
        _clear.onClick.AddListener(() => _segmentCache.GetSegment(_selectedSegment).Clear());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(SegmentCache cache, int minIntensity, int maxIntensity)
    {
        _segmentCache = cache;
        _selectedColor.color = cache.GetSegment(_selectedSegment).GetColor();
        _minRange.MinimumValue = minIntensity;
        _maxRange.MinimumValue = minIntensity;
        _minRange.MaximumValue = maxIntensity;
        _maxRange.MaximumValue = maxIntensity;
        _thresholdRegion.MinimumValue = 0;
        _thresholdRegion.MaximumValue = (maxIntensity - minIntensity) / 2f;

        for (int i = 0; i < SegmentCache.MaxSegmentCount; i++)
        {
            _selectedType[i] = SegmentationType.Range;
            _rangeParameters[i] = new RangeSegmentation.RangeParameter(minIntensity, maxIntensity, 2);
            _regionFillParameters[i] = new RegionFillSegmentation.RegionFillParameter(-1, -1, -1);
        }

        UpdateRegionSeed(-1, -1, -1);
        ValidateCurrentParameters();
    }

    private void ValidateCurrentParameters()
    {
        switch (_selectedType[_selectedSegment])
        {
            case SegmentationType.Range:
                _create.interactable = _rangeParameters[_selectedSegment] != null && _rangeParameters[_selectedSegment].IsValid();
                break;
            case SegmentationType.RegionFill:
                _create.interactable = _regionFillParameters[_selectedSegment] != null && _regionFillParameters[_selectedSegment].IsValid();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CreateSelection()
    {
        switch (_selectedType[_selectedSegment])
        {
            case SegmentationType.Range:
                _segmentCache.CreateSegment(SegmentCache.GetSelector(_selectedSegment), _rangeSegmentation, _rangeParameters[_selectedSegment]);
                break;
            case SegmentationType.RegionFill:
                _segmentCache.CreateSegment(SegmentCache.GetSelector(_selectedSegment), _regionFillSegmentation, _regionFillParameters[_selectedSegment]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UpdateRegionSeed(int x, int y, int z)
    {
        _regionFillParameters[_selectedSegment].X = x;
        _regionFillParameters[_selectedSegment].Y = y;
        _regionFillParameters[_selectedSegment].Z = z;

        _seedXRegion.text = "x: " + x;
        _seedYRegion.text = "y: " + y;
        _seedZRegion.text = "z: " + z;

        ValidateCurrentParameters();
    }

    private void ShowSeg(int index)
    {
        for (int i = 0; i < SegmentCache.MaxSegmentCount; i++)
        {
            _segmentButtons[i].interactable = i != index;
        }

        _selectedSegment = index;
        _selectedColor.color = _segmentCache.GetSegment(_selectedSegment).GetColor();
        ValidateCurrentParameters();
    }

    private void SelectedType(int index)
    {
        switch (_segmentationStrategyChoice.captionText.text)
        {
            case "Range":
                _regionFillParent.SetActive(false);
                _rangeParent.SetActive(true);
                _selectedType[_selectedSegment] = SegmentationType.Range;
                break;
            case "Region Fill":
                _regionFillParent.SetActive(true);
                _rangeParent.SetActive(false);
                _selectedType[_selectedSegment] = SegmentationType.RegionFill;
                break;
        }

        ValidateCurrentParameters();
    }

    private enum SegmentationType
    {
        Range, RegionFill
    }
}
