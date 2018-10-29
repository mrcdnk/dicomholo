using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DICOMData;
using UnityEngine;
using UnityEngine.UI;

public class Slice2DView : MonoBehaviour
{
    public ImageStack imageStack;
    public Slider sliceSlider;
    public Button transButton;
    public Button frontButton;
    public Button sagButton;

    private RawImage display;

    private readonly Dictionary<SliceType, int> selection = new Dictionary<SliceType, int>();

    private SliceType current = SliceType.TRANSVERSAL;
	// Use this for initialization
	void Start ()
	{
	    display = GetComponentInChildren<RawImage>();

	    foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
	    {
	        selection[type] = 0;
	    }

	    if (imageStack != null && imageStack.HasData(current))
	    {
	        display.texture = imageStack.GetTexture2D(current, selection[current]);       
	    }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ShowTrans()
    {
        transButton.interactable = false;
        frontButton.interactable = true;
        sagButton.interactable = true;
        Show(SliceType.TRANSVERSAL);
    }

    public void ShowFront()
    {
        transButton.interactable = true;
        frontButton.interactable = false;
        sagButton.interactable = true;
        Show(SliceType.FRONTAL);
    }

    public void ShowSag()
    {
        transButton.interactable = true;
        frontButton.interactable = true;
        sagButton.interactable = false;
        Show(SliceType.SAGITTAL);
    }

    public void Show(SliceType type)
    {
        current = type;
        sliceSlider.maxValue = imageStack.GetMaxValue(current);
        sliceSlider.value = selection[current];
        display.texture = imageStack.GetTexture2D(current, selection[current]);
    }

    public void SelectionChanged()
    {
        selection[current] = (int)sliceSlider.value;
        Texture2D tex = imageStack.GetTexture2D(current, selection[current]);
        display.texture = tex;
    }

}
