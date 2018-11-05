using System.Collections;
using System.Collections.Generic;
using DICOMData;
using UnityEngine;
using UnityEngine.UI;

public class OrthoSlices : MonoBehaviour
{
    private ImageStack imageStack;

    private RawImage transImage;
    private RawImage frontImage;
    private RawImage sagImage;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void initialize(ImageStack stack)
    {
        this.imageStack = stack;
    }
}
