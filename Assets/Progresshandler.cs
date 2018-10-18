using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Progresshandler : MonoBehaviour {

    private float max = 0;

    public Image foreground;
    public Text status;

    private string task = "";

    private float value = 0;

    public float getProgress()
    {
        if (foreground != null)
        {
            return foreground.fillAmount * max;
        }
        else
        {
            return 0;
        }
}

    public float increment(float add)
    {
        value += add;

        if (foreground != null)
        {
            foreground.fillAmount = value / max;
            updateText(value);

            return foreground.fillAmount;
        }

        return 0;
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(float max, string task)
    {
        this.max = max;
        foreground.fillAmount = 0f;
        updateText(value);
    }

    private void updateText(float value)
    {
        if (value / max < 1.0)
        {
            status.text = task + " " + value + " / " + (int) max;
        }
        else
        {
            status.text = "Done";
        }
    }
}
