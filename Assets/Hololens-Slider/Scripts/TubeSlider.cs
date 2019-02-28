using System;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using Cursor = HoloToolkit.Unity.InputModule.Cursor;

/// <summary>
/// Script handling a TubeSlider
/// </summary>
public class TubeSlider : MonoBehaviour
{
    public SliderChangedEvent SliderChangedEvent = new SliderChangedEvent();

    public string SliderName;

    public double MinimumValue
    {
        get { return _minValue; }
        set
        {
            _minValue = value;
            _sliderRange = MaximumValue - MinimumValue;
            UpdatePosition();
        }
    }
    public string MinimumLabel;

    public double MaximumValue
    {
        get { return _maxValue; }
        set
        {
            _maxValue = value;
            _sliderRange = MaximumValue - MinimumValue;
            UpdatePosition();
        }
    }
    public string MaximumLabel;

    public int CurrentInt
    {
        get { return Mathf.RoundToInt((float)((_sliderRange * _currentValue) + MinimumValue)); }
        set
        {
            _currentValue = (value - _minValue)/_sliderRange;
            UpdatePosition();
        }
    }

    public float CurrentFloat
    {
        get { return (float) (_currentValue*_sliderRange + MinimumValue); }
        set
        {
            _currentValue = (value - _minValue) / _sliderRange;
            UpdatePosition();
        }
    }

    public double CurrentDouble
    {
        get { return (_currentValue * _sliderRange + MinimumValue); }
        set
        {
            _currentValue = (value - _minValue) / _sliderRange;
            UpdatePosition();
        }
    }

    public double CurrentPercentage
    {
        get { return _currentValue; }
        private set { _currentValue = value; SliderChangedEvent.Invoke(this); }
    }

    public Color ButtonColorOffFocus
    {
        get { return _buttonColorOffFocus; }
        set
        {
            _buttonColorOffFocus = value;
            for (int i = 0; i < 4; ++i)
                ButtonColor.ButtonColorOffFocusArr[i] = _buttonColorOffFocus[i];
        }
    }

    public Color ButtonColorOnFocus
    {
        get { return _buttonColorOnFocus; }
        set
        {
            _buttonColorOnFocus = value;
            for (int i = 0; i < 4; ++i)
                ButtonColor.ButtonColorOnFocusArr[i] = _buttonColorOnFocus[i];
        }
    }

    public bool DisplayInt = true;

    public Stats ButtonColor;
    public Transform LeftPivot;
    public Transform RightPivot;

    private Cursor _cursor;

    [SerializeField] private Color _buttonColorOffFocus;
    [SerializeField] private Color _buttonColorOnFocus;

    [SerializeField]
    private double _minValue = 0;
    [SerializeField]
    private double _maxValue = 100;
    [SerializeField]
    private double _currentValue = 0.5d;

    private GameObject _button;
    private GameObject _buttonPivot;

    private string _leftLabel;
    private string _rightLabel;
    private string _buttonLabel;

    private bool _isSliderManipulationTriggered;

    private double _sliderRange;

    private Vector3 _start;
    private Vector3 _end;
    private Vector3 _sliderVector;
    private Vector3 _prevPosition;
    private Vector3 _movementDistance;
    private Vector3 _newPosition;
    private float _angleMinBound;
    private float _angleMaxBound;
    private Vector3 _newPositionVector;

    private void Awake()
    {
        _isSliderManipulationTriggered = false;
        foreach (Transform child in transform)
        {
            if (child.CompareTag("SliderButton"))
            {
                _button = child.gameObject;
            }
            else if (child.CompareTag("SliderName"))
            {
                child.GetComponent<TextMesh>().text = SliderName;
            }
        }

        _cursor = GameObject.FindGameObjectWithTag("HoloCursor").GetComponent<Cursor>();

        if (!_cursor)
        {
            Debug.LogWarning("No Cursor with Tag 'HoloCursor' present in scene.");
        }

        _sliderRange = MaximumValue - MinimumValue;

        _button.GetComponent<Renderer>().material.color = _buttonColorOffFocus;
        UpdatePosition();
    }

    private void OnValidate()
    {
        if (!_button || !LeftPivot || !RightPivot) return;

        _currentValue = Math.Max(0.0, Math.Min(_currentValue, 1.0));
        UpdatePosition();
    }

    /// <summary>
    /// Updates the position of the button on the slider.
    /// </summary>
    /// <param name="invokeEvent">defaults to true and controls if the value changed event should be invoked</param>
    private void UpdatePosition(bool invokeEvent = true)
    {
        _start = LeftPivot.position;
        _end = RightPivot.position;
        _sliderVector = _end - _start;

        _button.transform.position = _start + (-_button.transform.up.normalized * (float)_currentValue * _sliderVector.magnitude);
        _button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();

        if (invokeEvent)
        {
            SliderChangedEvent.Invoke(this);
        }
    }

    /// <summary>
    /// Handles the Cylinder Clicked message from the ClickHandler.
    /// </summary>
    public void CylinderClicked()
    {
        CylinderClicked(null);
    }

    /// <summary>
    /// Handles the Cylinder Clicked message from the ClickHandler.
    /// </summary>
    /// <param name="eventData"></param>
    public void CylinderClicked(InputClickedEventData eventData)
    {
        _start = LeftPivot.position;
        _end = RightPivot.position;
        _sliderVector = _end - _start;

        if (!_cursor) return;

        _movementDistance = Vector3.Project(_cursor.transform.position - _start, _sliderVector.normalized);
        _newPosition = _start + _movementDistance;

        _newPositionVector = _movementDistance;

        var clickedValue = _newPositionVector.magnitude/_sliderVector.magnitude;

        _angleMinBound = AngleDir(transform.forward, _newPositionVector, transform.up);
        _angleMaxBound = AngleDir(transform.forward, _end - _newPosition, transform.up);

        if (_angleMinBound == -1f || _angleMaxBound == -1f || !(clickedValue >= 0) || !(clickedValue <= 1)) return;

        _button.transform.position = _newPosition;
        CurrentPercentage = clickedValue;
    }

    /// <summary>
    /// Button was grabbed
    /// </summary>
    /// <param name="eventData"></param>
    public void ManipulationStarted(ManipulationEventData eventData)
    {
        if (_isSliderManipulationTriggered) return;

        _button.GetComponent<Renderer>().material.color = _buttonColorOnFocus;

        InputManager.Instance.PushModalInputHandler(_button);

        _prevPosition = _button.transform.position;

        ShowLabels();

        _isSliderManipulationTriggered = true;
    }

    /// <summary>
    /// Button is being moved
    /// </summary>
    /// <param name="eventData"></param>
    public void ManipulationUpdated(ManipulationEventData eventData)
    {
        _start = LeftPivot.position;
        _end = RightPivot.position;
        _sliderVector = _end - _start;

        if (!_isSliderManipulationTriggered) return;

        _button.GetComponent<Renderer>().material.color = _buttonColorOnFocus;

        _movementDistance = Vector3.Project(eventData.CumulativeDelta, _sliderVector.normalized);
        _newPosition = _prevPosition + _movementDistance;

        _newPositionVector = _newPosition - _start;

        _angleMinBound = AngleDir(transform.forward, _newPositionVector, transform.up);
        _angleMaxBound = AngleDir(transform.forward, _end - _newPosition, transform.up);

        if (_angleMinBound != -1f && _angleMaxBound != -1f)
        {
            _button.transform.position = _newPosition;
            CurrentPercentage = _newPositionVector.magnitude/_sliderVector.magnitude;
        }
            
        ShowLabels();
    }

    /// <summary>
    /// Button was released
    /// </summary>
    /// <param name="eventData"></param>
    public void ManipulationCompleted(ManipulationEventData eventData)
    {
        if (!_isSliderManipulationTriggered) return;

        _button.GetComponent<Renderer>().material.color = _buttonColorOffFocus;
        HideLabels();

        InputManager.Instance.PopModalInputHandler();

        _isSliderManipulationTriggered = false;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    public void ManipulationCanceled(ManipulationEventData eventData)
    {
        if (!_isSliderManipulationTriggered) return;

        _button.GetComponent<Renderer>().material.color = _buttonColorOffFocus;
        HideLabels();

        InputManager.Instance.PopModalInputHandler();

        _isSliderManipulationTriggered = false;
    }

    /// <summary>
    /// Button received focus
    /// </summary>
    public void ButtonOnFocus()
    {
        _button.GetComponent<Renderer>().material.color = _buttonColorOnFocus;
        
        _button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();
    }

    /// <summary>
    /// Button removed from focus
    /// </summary>
    public void ButtonOffFocus()
    {
        if (!_isSliderManipulationTriggered)
        {
            _button.GetComponent<Renderer>().material.color = _buttonColorOffFocus;

        }
    }

    /// <summary>
    /// Sets the min and max Labels.
    /// </summary>
    public void ShowLabels()
    {

        LeftPivot.GetChild(0).GetComponent<TextMesh>().text = MinimumLabel;
        RightPivot.GetChild(0).GetComponent<TextMesh>().text = MaximumLabel;
 
        _button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();      
    }

    /// <summary>
    /// Hides the min and max labels
    /// </summary>
    public void HideLabels()
    {
        LeftPivot.GetChild(0).GetComponent<TextMesh>().text = "";
        RightPivot.GetChild(0).GetComponent<TextMesh>().text = "";
    }

    /// <summary>
    /// Returns the current value as string
    /// </summary>
    /// <returns></returns>
    private string GetCurrentValueAsString()
    {
        return DisplayInt ? CurrentInt.ToString() : CurrentFloat.ToString("N2");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fwd"></param>
    /// <param name="targetDir"></param>
    /// <param name="up"></param>
    /// <returns></returns>
    private static float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        var perp = Vector3.Cross(fwd, targetDir);
        var dir = Vector3.Dot(perp, up);

        if (dir > 0f)
        {
            return 1f;
        }

        if (dir < 0f)
        {
            return -1f;
        }

        return 0f;
    }
}


[Serializable]
public class Stats
{
    public float[] ButtonColorOffFocusArr;
    public float[] ButtonColorOnFocusArr;
    public Stats()
    {
        ButtonColorOffFocusArr = new float[4];
        ButtonColorOnFocusArr = new float[4];
    }
}