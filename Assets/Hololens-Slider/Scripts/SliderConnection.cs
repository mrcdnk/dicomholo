using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderConnection : MonoBehaviour {

    [Tooltip("Drag the slider object to link the values")]
    public TubeSlider Slider;

	public int Value;

	// Use this for initialization
	void Awake () {
		if(Slider == null)
		{
			return;
		}        

        Value = Slider.CurrentInt;

        gameObject.SendMessage("GetSliderValue", Value);
	}
	
	// Update is called once per frame
	void Update () {
		Value = Slider.CurrentInt;
        gameObject.SendMessage("GetSliderValue", Value);
    }
}
