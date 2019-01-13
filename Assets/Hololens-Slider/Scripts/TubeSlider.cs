using HoloToolkit.Unity.InputModule;
using UnityEngine;
using Cursor = HoloToolkit.Unity.InputModule.Cursor;

public class TubeSlider : MonoBehaviour
{
    private Cursor Cursor;
    public SliderChangedEvent SliderChangedEvent = new SliderChangedEvent();

    public string SliderName;

    [SerializeField]
    private float _sliderMinValue = 0;

    public float SliderMinimumValue
    {
        get { return _sliderMinValue; }
        set
        {
            _sliderMinValue = value;
            SliderRange = SliderMaximumValue - SliderMinimumValue;
        }
    }
    public string SliderMinimumLabel;

    [SerializeField]
    private float _sliderMaxValue = 100;

    public float SliderMaximumValue
    {
        get { return _sliderMaxValue; }
        set
        {
            _sliderMaxValue = value;
            SliderRange = SliderMaximumValue - SliderMinimumValue;
        }
    }
    public string SliderMaximumLabel;

    [SerializeField]
    private float _currentValue = 0.5f;

    public int CurrentInt
    {
        get { return Mathf.RoundToInt((SliderRange * _currentValue) + SliderMinimumValue); }
        set
        {
            _currentValue = (value - _sliderMinValue)/SliderRange;
            button.transform.position = start + (-button.transform.up.normalized * _currentValue * sliderVector.magnitude);
            button.GetComponentInChildren<TextMesh>().text = GetCurrentValueAsString();
            SliderChangedEvent.Invoke(this);
        }
    }

    public float CurrentFloat => (_currentValue * SliderRange) + SliderMinimumValue;

    public float CurrentPercentage
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

    private float SliderRange;

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

        SliderRange = SliderMaximumValue - SliderMinimumValue;

        start = LeftPivot.position;
        end = RightPivot.position;

        sliderVector = end - start;

        button.transform.position = start + (-button.transform.up.normalized * _currentValue * sliderVector.magnitude);

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

        LeftPivot.GetChild(0).GetComponent<TextMesh>().text = SliderMinimumLabel;
        RightPivot.GetChild(0).GetComponent<TextMesh>().text = SliderMaximumLabel;
 
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