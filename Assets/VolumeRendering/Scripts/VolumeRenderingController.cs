using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace VolumeRendering
{

    public class VolumeRenderingController : MonoBehaviour {

        [SerializeField] protected VolumeRendering volume;
        [SerializeField] protected Slider sliderXMin, sliderXMax, sliderYMin, sliderYMax, sliderZMin, sliderZMax;

        void Start ()
        {
            const float threshold = 0.025f;

            sliderXMin.onValueChanged.AddListener((v) => {
                volume.SliceXMin = sliderXMin.value = Mathf.Min(v, volume.SliceXMax - threshold);
            });
            sliderXMax.onValueChanged.AddListener((v) => {
                volume.SliceXMax = sliderXMax.value = Mathf.Max(v, volume.SliceXMin + threshold);
            });
            
            sliderYMin.onValueChanged.AddListener((v) => {
                volume.SliceYMin = sliderYMin.value = Mathf.Min(v, volume.SliceYMax - threshold);
            });
            sliderYMax.onValueChanged.AddListener((v) => {
                volume.SliceYMax = sliderYMax.value = Mathf.Max(v, volume.SliceYMin + threshold);
            });

            sliderZMin.onValueChanged.AddListener((v) => {
                volume.SliceZMin = sliderZMin.value = Mathf.Min(v, volume.SliceZMax - threshold);
            });
            sliderZMax.onValueChanged.AddListener((v) => {
                volume.SliceZMax = sliderZMax.value = Mathf.Max(v, volume.SliceZMin + threshold);
            });

        }


        public void OnIntensity(float v)
        {
            volume.Intensity = v;
        }

    }

}


