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

        public const string FolderHint = "Select Folder";

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
            _selection.AddOptions(new List<string> {FolderHint });
        }

        public void AddDropdownOptions(List<string> options)
        {
            _selection.AddOptions(options);
        }

        public void RemoveHint()
        {
            if (_selection.options.RemoveAll(item => item.text == FolderHint) > 0)
            {
                //_selection.value -= 1;
            }
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
