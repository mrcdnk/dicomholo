using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Progresshandler : MonoBehaviour {

    private float max = 0;

    public Image foreground;
    public Text status;

    public int value
    {
        get
        {
            if (foreground != null)
            {
                return (int)(foreground.fillAmount * max);
            }
            else
            {
                return 0;
            }
        }
        set
        {
            if (foreground != null)
            {
                foreground.fillAmount = value / max;
                updateText(value);
            }
        }
    }


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(int max)
    {
        this.max = max;
        foreground.fillAmount = 0f;
        updateText(value);
    }

    private void updateText(int value)
    {
        status.text = value + " / " + (int)max;
    }
}
