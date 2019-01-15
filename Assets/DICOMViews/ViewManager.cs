using System.Collections.Generic;
using DICOMParser;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    public class ViewManager : MonoBehaviour
    {

        public WindowSettingsPanel WindowSettingsPanel;

        public List<Button> disabledButtons;

        private ImageStack stack;

        public Slice2DView Slice2DView;

        public VolumeRendering.VolumeRendering VolumeRendering;
        public GameObject Volume;


        // Use this for initialization
        void Start ()
        {
            stack = gameObject.AddComponent<ImageStack>();
            Volume.SetActive(false);
            Slice2DView.gameObject.SetActive(false);
        }
	
        // Update is called once per frame
        void Update () {
		
        }

    }
}
