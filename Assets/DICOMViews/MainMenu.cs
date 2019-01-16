using System.Collections;
using Threads;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    public class MainMenu : MonoBehaviour
    {
        public Dropdown Selection;
        public ProgressHandler ProgressHandler;
        public RawImage PreviewImage;

        public Button LoadVolumeButton;
        public Button Load2DButton;

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void SetPreviewImage(Texture2D texture2D)
        {
            PreviewImage.texture = texture2D;
        }

        public void DisableButtons()
        {
            LoadVolumeButton.enabled = false;
            Load2DButton.enabled = false;
        }

        public void EnableButtons()
        {
            LoadVolumeButton.enabled = true;
            Load2DButton.enabled = true;
        }
    }
}
