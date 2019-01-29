using System.Collections;
using System.Collections.Generic;
using Threads;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        private Dropdown _selection;

        public ProgressHandler ProgressHandler;
        public RawImage PreviewImage;

        public Button LoadVolumeButton;
        public Button Load2DButton;

        public void SetPreviewImage(Texture2D texture2D)
        {
            PreviewImage.texture = texture2D;
        }

        public string GetSelectedFolder()
        {
            return _selection.captionText.text;
        }

        public void ClearDropdown()
        {
            _selection.ClearOptions();
        }

        public void AddDropdownOptions(List<string> options)
        {
            _selection.AddOptions(options);
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
