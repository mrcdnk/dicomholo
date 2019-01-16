using UnityEngine;
using UnityEngine.UI;

public class ProgressHandler : MonoBehaviour {

    public Image Foreground;
    public Text Status;

    [SerializeField]
    private float _value = 0;
    public float Value
    {
        get { return _value; }
        set
        {
            _value = Mathf.Max(Mathf.Min(value, _max), 0);
            UpdateProgress();
        }
    }

    private float _max = 0;
    public float Max
    {
        get { return _max; }
        set
        {
            _max = value;
            UpdateProgress();
        }
    }

    private string _task = "";
    public string TaskDescription
    {
        get { return _task; }
        set
        {
            _task = value;
            UpdateText();
        }
    }

    public float Progress
    {
        get
        {
            if (Foreground != null)
            {
                return Foreground.fillAmount * _max;
            }

            return 0;
        }
    }

    /// <summary>
    /// Increments the progress limited to the configured max value.
    /// </summary>
    /// <param name="add">The amount of progress to add.</param>
    /// <returns></returns>
    public float Increment(float add)
    {
        _value += add;
        _value = Mathf.Min(_value, _max);
        return UpdateProgress();
    }

    /// <summary>
    /// Recalculates progress and updates the text afterwards.
    /// </summary>
    /// <returns>The current progress in percent between 0 and 1.</returns>
    private float UpdateProgress()
    {
        if (Foreground != null)
        {
            Foreground.fillAmount = _value / _max;
            UpdateText();

            return Foreground.fillAmount;
        }

        return 0;
    }

    /// <summary>
    /// Initializes the ProgressHandler with base value 0.
    /// </summary>
    /// <param name="max">Number of Steps to reach 100%, must be > 0.</param>
    /// <param name="task">Short description of the current task.</param>
    public void Init(float max, string task)
    {
        _max = max;
        _value = 0;
        _task = task;
        Foreground.fillAmount = 0f;
        UpdateText();
    }

    /// <summary>
    /// Updates the displayed Text
    /// </summary>
    private void UpdateText()
    {
        if (_value / _max < 1.0)
        {
            Status.text = _task + " " + _value + " / " + (int) _max;
        }
        else
        {
            Status.text = "Finished " + _task;
        }
    }
}
