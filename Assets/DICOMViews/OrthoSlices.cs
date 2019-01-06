
using DICOMData;
using UnityEngine;
using UnityEngine.UI;

public class OrthoSlices : MonoBehaviour
{
    private ImageStack imageStack;

    public GameObject transImage;
    private float transStep;
    private GameObject frontImage;
    private GameObject sagImage;

    public Slider transSlider;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void initialize(ImageStack stack)
    {
        this.imageStack = stack;
        transStep = 1f/(stack.GetMaxValue(SliceType.TRANSVERSAL));
        transSlider.maxValue = stack.GetMaxValue(SliceType.TRANSVERSAL);
    }

    public void onTransSliderChanged()
    {
        Vector3 local = transImage.GetComponent<Transform>().localPosition;

        local.y = transSlider.value * transStep;

        transImage.GetComponent<Transform>().localPosition = local;
        //Sprite current = transImage.GetComponent<Sprite>();
        SpriteRenderer currentRenderer = transImage.GetComponent<SpriteRenderer>();

        currentRenderer.material.mainTexture = imageStack.GetTexture2D(SliceType.TRANSVERSAL, (int)transSlider.value);
        currentRenderer.material.shader = Shader.Find("Sprites/Transparent Unlit");
    }
}
