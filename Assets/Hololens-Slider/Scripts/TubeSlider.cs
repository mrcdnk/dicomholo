using System;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using Cursor = HoloToolkit.Unity.InputModule.Cursor;

public class TubeSlider : MonoBehaviour
{
    private Cursor Cursor;
    public SliderChangedEvent SliderChangedEvent = new SliderChangedEvent();

    public string SliderName;

    [SerializeField]
    private double _minValue = 0;

    public double MinimumValue
    {
        get { return _minValue; }
        set
        {
            _minValue = value;
            SliderRange = MaximumValue - MinimumValue;
        }
    }
    public string MinimumLabel;

    [SerializeField]
    private double _maxValue = 100;

    public double MaximumValue
    {
        get { return _maxValue; }
        set
        {
            _maxValue = value;
            SliderRange = MaximumValue - MinimumValue;
        }
    }
    public string MaximumLabel;

    [SerializeField]
    private double _currentValue = 0.5d;

    public int CurrentInt
    {
        get { return Mathf.RoundToInt((float)((SliderRange * _currentValue) + MinimumValue)); }
        set
        {
            _currentValue = (value - _minValue)/SliderRange;
            button.transform.position = start + (-button.transform.up.normalized * (float)_currentValue * sliderVector.magnitude);
            button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();
            SliderChangedEvent.Invoke(this);
        }
    }

    public float CurrentFloat => (float)((_currentValue * SliderRange) + MinimumValue);

    public double CurrentDouble => _currentValue;

    public double CurrentPercentage
    {
        get { return _currentValue; }
        private set { _currentValue = value; SliderChangedEvent.Invoke(this); }
    }

    public bool DisplayInt = true;

    public Stats ButtonColor;
    public Color ButtonColorOffFocus;
    public Color ButtonColorOnFocus;

    public Transform LeftPivot;
    public Transform RightPivot;
    private GameObject button;
    private GameObject buttonPivot;

    private string leftLabel;
    private string rightLabel;
    private string buttonLabel;

    private bool isSliderManipulationTriggered;

    private double SliderRange;

    private Vector3 start;
    private Vector3 end;
    private Vector3 sliderVector;
    private Vector3 prevPosition;
    private Vector3 movementDistance;
    private Vector3 newPosition;
    private float angleMinBound;
    private float angleMaxBound;
    private Vector3 newPositionVector;

    void Awake()
    {
        isSliderManipulationTriggered = false;
        foreach (Transform child in transform)
        {
           
            switch(child.tag)
            {
                case "SliderButton":
                    button = child.gameObject;
                    break;
                case "SliderName":
                    child.GetComponent<TextMesh>().text = SliderName;
                    break;
            }
        }

        Cursor = GameObject.FindGameObjectWithTag("HoloCursor").GetComponent<Cursor>();

        if (!Cursor)
        {
            Debug.LogWarning("No Cursor with Tag 'HoloCursor' present in scene.");
        }

        SliderRange = MaximumValue - MinimumValue;

        start = LeftPivot.position;
        end = RightPivot.position;

        sliderVector = end - start;

        button.transform.position = start + (-button.transform.up.normalized * (float)_currentValue * sliderVector.magnitude);

        button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();
        button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;
        SliderChangedEvent.Invoke(this);
    }

    public Color buttonColorOffFocus
    {
        get { return ButtonColorOffFocus; }
        set
        {
            ButtonColorOffFocus = value;
            for (int i = 0; i < 4; ++i)
                ButtonColor.ButtonColorOffFocusArr[i] = ButtonColorOffFocus[i];
        }
    }

    public Color buttonColorOnFocus
    {
        get { return ButtonColorOnFocus; }
        set
        {
            ButtonColorOnFocus = value;
            for (int i = 0; i < 4; ++i)
                ButtonColor.ButtonColorOnFocusArr[i] = ButtonColorOnFocus[i];
        }
    }

    public void CylinderClicked(InputClickedEventData eventData)
    {
        if (Cursor)
        {
            movementDistance = Vector3.Project(Cursor.transform.position - start, sliderVector.normalized);
            newPosition = start + movementDistance;

            newPositionVector = movementDistance;

            var clickedValue = newPositionVector.magnitude/sliderVector.magnitude;

            angleMinBound = AngleDir(transform.forward, newPositionVector, transform.up);
            angleMaxBound = AngleDir(transform.forward, end - newPosition, transform.up);

            if (angleMinBound != -1f && angleMaxBound != -1f && clickedValue >= 0 && clickedValue <= 1)
            {
                button.transform.position = newPosition;
                CurrentPercentage = clickedValue;
            }
        }
    }

    public void ManipulationStarted(ManipulationEventData eventData)
    {
        if (isSliderManipulationTriggered) return;

        button.GetComponent<Renderer>().material.color = ButtonColorOnFocus;

        InputManager.Instance.PushModalInputHandler(button);

        prevPosition = button.transform.position;

        ShowLabels();

        isSliderManipulationTriggered = true;
    }

    public void ManipulationUpdated(ManipulationEventData eventData)
    {
        if (!isSliderManipulationTriggered) return;

        button.GetComponent<Renderer>().material.color = ButtonColorOnFocus;

        movementDistance = Vector3.Project(eventData.CumulativeDelta, sliderVector.normalized);
        newPosition = prevPosition + movementDistance;

        newPositionVector = newPosition - start;

        angleMinBound = AngleDir(transform.forward, newPositionVector, transform.up);
        angleMaxBound = AngleDir(transform.forward, end - newPosition, transform.up);

        if (angleMinBound != -1f && angleMaxBound != -1f)
        {
            button.transform.position = newPosition;
            CurrentPercentage = newPositionVector.magnitude/sliderVector.magnitude;
        }
            
        ShowLabels();
    }

    public void ManipulationCompleted(ManipulationEventData eventData)
    {
        if (isSliderManipulationTriggered)
        {
            button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;
            HideLabels();

            InputManager.Instance.PopModalInputHandler();

            isSliderManipulationTriggered = false;
        }

    }

    public void ManipulationCanceled(ManipulationEventData eventData)
    {
        if (isSliderManipulationTriggered)
        {
            button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;
            HideLabels();

            InputManager.Instance.PopModalInputHandler();

            isSliderManipulationTriggered = false;
        }
    }

    public void ButtonOnFocus()
    {
        button.GetComponent<Renderer>().material.color = ButtonColorOnFocus;
        
        button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();
    }

    public void ButtonOffFocus()
    {
        if (!isSliderManipulationTriggered)
        {
            button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;

        }
    }

    public void ShowLabels()
    {

        LeftPivot.GetChild(0).GetComponent<TextMesh>().text = MinimumLabel;
        RightPivot.GetChild(0).GetComponent<TextMesh>().text = MaximumLabel;
 
        button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();      
    }

    private string GetCurrentValueAsString()
    {
        return DisplayInt ? CurrentInt.ToString() : CurrentFloat.ToString("N2");
    }

    public void HideLabels()
    {
        LeftPivot.GetChild(0).GetComponent<TextMesh>().text = "";
        RightPivot.GetChild(0).GetComponent<TextMesh>().text = "";
    }

    float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, up);

        if (dir > 0f)
        {
            return 1f;
        }
        else if (dir < 0f)
        {
            return -1f;
        }
        else
        {
            return 0f;
        }
    }
}


[System.Serializable]
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