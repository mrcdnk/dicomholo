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
        private Dropdown _selection = null;

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
            LoadVolumeButton.interactable = false;
            Load2DButton.interactable = false;
        }

        public void EnableButtons()
        {
            LoadVolumeButton.interactable = true;
            Load2DButton.interactable = true;
        }
    }
}
