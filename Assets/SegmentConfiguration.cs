using System.Collections;
using System.Collections.Generic;
using Segmentation;
using UnityEngine;
using UnityEngine.UI;

public class SegmentConfiguration : MonoBehaviour
{
    [SerializeField] private Button _segment1;
    [SerializeField] private Button _segment2;
    [SerializeField] private Button _segment3;

    [SerializeField] private Button[] _segmentButtons;

    [SerializeField] private Button _clear;
    [SerializeField] private Button _create;

    [SerializeField] private Text _selectedName;
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

    private RegionFillSegmentation _regionFillSegmentation = new RegionFillSegmentation();
    private RangeSegmentation _rangeSegmentation = new RangeSegmentation();

    private int _selectedSegment = 0;

    private SegmentCache _segmentCache;

    // Start is called before the first frame update
    void Start()
    {
        _regionFillParent.SetActive(false);
        _rangeParent.SetActive(true);

        for (int i = 0 ; i< _segmentButtons.Length; i++)
        {
            _segmentButtons[i].onClick.AddListener(() => ShowSeg(i));
        }

        _segmentationStrategyChoice.onValueChanged.AddListener(SelectedType);

        _create.onClick.AddListener(CreateSelection);
        _clear.onClick.AddListener(() => _segmentCache.GetSegment(_selectedSegment).Clear());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(SegmentCache cache)
    {
        _segmentCache = cache;
        _selectedColor.color = cache.GetSegment(_selectedSegment).GetColor();
    }

    private void CreateSelection()
    {

    }

    private void ShowSeg(int index)
    {
        for (int i = 0; i < _segmentButtons.Length; i++)
        {
            _segmentButtons[i].interactable = i == index;
        }

        _selectedName.text = "Segment "+(index+1);
        _selectedSegment = index;
        _selectedColor.color = _segmentCache.GetSegment(_selectedSegment).GetColor();
    }

    private void SelectedType(int index)
    {
        switch (_segmentationStrategyChoice.captionText.text)
        {
            case "Range":
                _regionFillParent.SetActive(false);
                _rangeParent.SetActive(true);
                break;
            case "Region Fill":
                _regionFillParent.SetActive(true);
                _rangeParent.SetActive(false);
                break;
        }
    }

}
