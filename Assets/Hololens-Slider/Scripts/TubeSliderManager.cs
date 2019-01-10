using HoloToolkit.Unity.InputModule;
using UnityEngine;
using Cursor = HoloToolkit.Unity.InputModule.Cursor;

public class TubeSliderManager : MonoBehaviour
{
    public Cursor Cursor;

    public string SliderName;

    [SerializeField]
    public uint SliderMinimumValue = 0;
    public string SliderMinimumLabel;



    [SerializeField]
    public uint SliderMaximumValue = 100;
    public string SliderMaximumLabel;

    public uint CurrentValue = 50;

    public bool isCurrentValueToBeDisplayed = true;

    public Stats ButtonColor;
    public Color ButtonColorOffFocus;
    public Color ButtonColorOnFocus;

    private Rigidbody rb;

    private GameObject leftHolder;
    private GameObject rightHolder;
    private GameObject button;
    private GameObject buttonPivot;

    private string leftLabel;
    private string rightLabel;
    private string buttonLabel;

    private bool isSliderManipulationTriggered;

    private uint SliderRange;

    private Vector3 start;
    private Vector3 end;
    private Vector3 sliderVector;
    private Vector3 prevPosition;
    private Vector3 movementDistance;
    private Vector3 newPosition;
    private float angleMinBound;
    private float angleMaxBound;
    private Vector3 newPositionVector;
    private float diff;

    void Awake()
    {
        isSliderManipulationTriggered = false;

        foreach (Transform child in transform)
        {
           
            switch(child.tag)
            {
                case "LeftHolder":
                    leftHolder = child.gameObject;
                    break;
                case "RightHolder":
                    rightHolder = child.gameObject;
                    break;
                case "SliderButton":
                    button = child.gameObject;
                    break;
                case "SliderName":
                    child.GetComponent<TextMesh>().text = SliderName;
                    break;
            }
        }

        SliderRange = SliderMaximumValue - SliderMinimumValue;

        start = leftHolder.transform.position;
        end = rightHolder.transform.position;

        sliderVector = end - start;

        diff = button.GetComponent<BoxCollider>().bounds.size.x;

        button.transform.position = start + (-button.transform.up.normalized * (((float)CurrentValue / (float)SliderRange)) * (sliderVector.magnitude));

        button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;

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

            var ClickedValue = (uint)Mathf.RoundToInt((float)SliderRange * ((newPositionVector.magnitude) / (sliderVector.magnitude)));

            angleMinBound = AngleDir(transform.forward, newPositionVector, transform.up);
            angleMaxBound = AngleDir(transform.forward, end - newPosition, transform.up);

            if (angleMinBound != -1f && angleMaxBound != -1f && ClickedValue >= SliderMinimumValue && ClickedValue <= SliderMaximumValue)
            {
                button.transform.position = newPosition;
                CurrentValue = ClickedValue;
            }
        }
    }

    public void ManipulationStarted(ManipulationEventData eventData)
    {
        if (isSliderManipulationTriggered) return;

        button.GetComponent<Renderer>().material.color = ButtonColorOnFocus;

        InputManager.Instance.PushModalInputHandler(button);

        rb = button.GetComponent<Rigidbody>();

        prevPosition = button.transform.position;

        setDisplay(SliderMinimumLabel, SliderMaximumLabel, CurrentValue.ToString());

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
            CurrentValue = (uint)Mathf.RoundToInt((float)SliderRange * ((newPositionVector.magnitude) / (sliderVector.magnitude)));
        }
            

        setDisplay(SliderMinimumLabel, SliderMaximumLabel, CurrentValue.ToString());

    }

    public void ManipulationCompleted(ManipulationEventData eventData)
    {
        if (isSliderManipulationTriggered)
        {
            button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;
            setDisplay("", "", "");

            InputManager.Instance.PopModalInputHandler();

            isSliderManipulationTriggered = false;
        }

    }

    public void ManipulationCanceled(ManipulationEventData eventData)
    {
        if (isSliderManipulationTriggered)
        {
            button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;
            setDisplay("", "", "");

            InputManager.Instance.PopModalInputHandler();

            isSliderManipulationTriggered = false;
        }
    }

    public void ButtonOnFocus()
    {
        button.GetComponent<Renderer>().material.color = ButtonColorOnFocus;
        if (isCurrentValueToBeDisplayed)
        {
            button.GetComponentInChildren<TextMesh>().text = CurrentValue.ToString();
        }

    }

    public void ButtonOffFocus()
    {
        if (!isSliderManipulationTriggered)
        {
            button.GetComponent<Renderer>().material.color = ButtonColorOffFocus;

        }

        button.GetComponentInChildren<TextMesh>().text = "";
    }

    public void setDisplay(string min, string max, string current)
    {

        leftHolder.transform.GetChild(0).GetComponent<TextMesh>().text = min;
        rightHolder.transform.GetChild(0).GetComponent<TextMesh>().text = max;


        if (isCurrentValueToBeDisplayed)
        {
            button.GetComponentInChildren<TextMesh>().text = current;
        }

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