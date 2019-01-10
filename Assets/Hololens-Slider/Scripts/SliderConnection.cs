using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderConnection : MonoBehaviour {

    [Tooltip("Drag the slider object to link the values")]
    public GameObject Slider;

	public uint Value;

    private TubeSliderManager linkedSlider;

	// Use this for initialization
	void Awake () {
		if(Slider == null)
		{
			return;
		}        

        linkedSlider = GameObject.Find(Slider.name).GetComponent<TubeSliderManager>();

        Value = linkedSlider.CurrentValue;

        gameObject.SendMessage("GetSliderValue", Value);
	}
	
	// Update is called once per frame
	void Update () {
		Value = linkedSlider.CurrentValue;
        gameObject.SendMessage("GetSliderValue", Value);
    }
}
