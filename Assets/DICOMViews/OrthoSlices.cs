using DICOMParser;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    /// <summary>
    /// Incomplete OrthoSlices View, efficient implementation in Unity is not easy.
    /// </summary>
    public class OrthoSlices : MonoBehaviour
    {
        public GameObject TransImage;

        private ImageStack _imageStack;
        private float _transStep;
        private GameObject _frontImage;
        private GameObject _sagImage;

        public Slider TransSlider;

        public void Initialize(ImageStack stack)
        {
            _imageStack = stack;
            _transStep = 1f / (stack.GetMaxValue(SliceType.Transversal));
            TransSlider.maxValue = stack.GetMaxValue(SliceType.Transversal);
        }

        public void OnTransSliderChanged()
        {
            var local = TransImage.GetComponent<Transform>().localPosition;

            local.y = TransSlider.value * _transStep;

            TransImage.GetComponent<Transform>().localPosition = local;
            //Sprite current = transImage.GetComponent<Sprite>();
            var currentRenderer = TransImage.GetComponent<SpriteRenderer>();

            currentRenderer.material.mainTexture = _imageStack.GetTexture2D(SliceType.Transversal, (int) TransSlider.value);
            currentRenderer.material.shader = Shader.Find("Sprites/Transparent Unlit");
        }
    }

}