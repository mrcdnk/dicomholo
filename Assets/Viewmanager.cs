using System.Collections;
using System.Collections.Generic;
using DICOMData;
using UnityEngine;
using UnityEngine.UI;

public class Viewmanager : MonoBehaviour
{

    public List<Button> disabledButtons;
    private ImageStack stack;

    public OrthoSlices ortho;

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

        ortho.initialize(stack);
    }

    public void createSimple2D()
    {

    }
}
