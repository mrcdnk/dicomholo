using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DICOMViews
{
    /// <summary>
    /// Main Menu Window
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public const string FolderHint = "Select Folder";

        public ProgressHandler ProgressHandler;
        public RawImage PreviewImage;

        public Button LoadVolumeButton;
        public Button Load2DButton;

        [SerializeField]
        private Dropdown _selection = null;

        /// <summary>
        /// Sets the preview Image shown in the main menu.
        /// </summary>
        /// <param name="texture2D">Texture containing the Image</param>
        public void SetPreviewImage(Texture2D texture2D)
        {
            PreviewImage.texture = texture2D;
        }

        /// <summary>
        /// Returns the captionText of the currently selected item.
        /// </summary>
        /// <returns>Either a folder name or the FolderHint.</returns>
        public string GetSelectedFolder()
        {
            return _selection.captionText.text;
        }

        /// <summary>
        /// Clears the Dropdown and adds the FolderHint
        /// </summary>
        public void ClearDropdown()
        {
            _selection.ClearOptions();
            _selection.AddOptions(new List<string> {FolderHint });
        }

        /// <summary>
        /// Adds the given Options to the dropdown.
        /// </summary>
        /// <param name="options">List of options.</param>
        public void AddDropdownOptions(List<string> options)
        {
            _selection.AddOptions(options);
        }

        /// <summary>
        /// Disables Buttons.
        /// </summary>
        public void DisableButtons()
        {
            LoadVolumeButton.interactable = false;
            Load2DButton.interactable = false;
        }


        /// <summary>
        /// Enables Buttons.
        /// </summary>
        public void EnableButtons()
        {
            LoadVolumeButton.interactable = true;
            Load2DButton.interactable = true;
        }

        /// <summary>
        /// Disables Dropdown.
        /// </summary>
        public void DisableDropDown()
        {
            _selection.interactable = false;
        }

        /// <summary>
        /// Enables Dropdown.
        /// </summary>
        public void EnableDropDown()
        {
            _selection.interactable = true;
        }
    }
}
