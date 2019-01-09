using System.Collections;
using System.Collections.Generic;
using DICOMParser;
using UnityEngine;
using UnityEngine.UI;

public class ViewManager : MonoBehaviour
{

    public List<Button> disabledButtons;
    private ImageStack stack;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ready(ImageStack stack)
    {
        this.stack = stack;
        /*foreach (Button button in disabledButtons)
        {
            button.interactable = true;
        }*/
    }

    public void createSimple2D()
    {

    }
}
