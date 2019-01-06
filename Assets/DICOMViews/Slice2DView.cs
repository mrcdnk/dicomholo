using System;
using System.Collections.Generic;
using System.Linq;
using DICOMParser;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{

    public class Slice2DView : MonoBehaviour
    {
        public ImageStack imageStack;
        public Slider sliceSlider;
        public Button transButton;
        public Button frontButton;
        public Button sagButton;

        private RawImage display;

        private readonly Dictionary<SliceType, int> selection = new Dictionary<SliceType, int>();

        private SliceType current = SliceType.Transversal;

        // Use this for initialization
        void Start()
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
        void Update()
        {

        }

        public void ShowTrans()
        {
            transButton.interactable = false;
            frontButton.interactable = true;
            sagButton.interactable = true;
            Show(SliceType.Transversal);
        }

        public void ShowFront()
        {
            transButton.interactable = true;
            frontButton.interactable = false;
            sagButton.interactable = true;
            Show(SliceType.Frontal);
        }

        public void ShowSag()
        {
            transButton.interactable = true;
            frontButton.interactable = true;
            sagButton.interactable = false;
            Show(SliceType.Sagittal);
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
            selection[current] = (int) sliceSlider.value;
            display.texture = imageStack.GetTexture2D(current, selection[current]);
        }

        public void TextureUpdated(SliceType type, int index)
        {
            if (current == type && selection[current] == index)
            {
                display.texture = imageStack.GetTexture2D(current, selection[current]);
            }
        }

        public SliceType GetCurrentSliceType()
        {
            return current;
        }

        public int GetSelection(SliceType type)
        {
            return selection[type];
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

    }
}
