using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.UI;

public class ClickHandler : MonoBehaviour, IInputClickHandler
{

    public void OnInputClicked(InputClickedEventData eventData)
    {
        SendMessageUpwards("CylinderClicked", eventData);
    }
}
