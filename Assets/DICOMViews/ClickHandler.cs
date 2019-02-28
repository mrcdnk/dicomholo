using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DICOMViews
{
    /// <summary>
    /// Receives clicks on the attached object and sends events upwards. Used for the HoloSlider.
    /// </summary>
    public class ClickHandler : MonoBehaviour, IInputClickHandler, IPointerClickHandler
    {

        public void OnInputClicked(InputClickedEventData eventData)
        {
            SendMessageUpwards("CylinderClicked", eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SendMessageUpwards("CylinderClicked", null);
        }
    }
}
