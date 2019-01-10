using HoloToolkit.Unity.InputModule;
using UnityEngine;

public class ButtonHandler : MonoBehaviour, IFocusable, IManipulationHandler
{

	public void OnFocusEnter() 
	{

		SendMessageUpwards ("ButtonOnFocus");
	}
	public void OnFocusExit() 
	{
		SendMessageUpwards ("ButtonOffFocus");
	}

    public void OnManipulationStarted(ManipulationEventData eventData)
    {
        SendMessageUpwards("ManipulationStarted", eventData);
    }

    public void OnManipulationUpdated(ManipulationEventData eventData)
    {
        SendMessageUpwards("ManipulationUpdated", eventData);

    }

    public void OnManipulationCompleted(ManipulationEventData eventData)
    {
        SendMessageUpwards("ManipulationCompleted", eventData);

    }

    public void OnManipulationCanceled(ManipulationEventData eventData)
    {
        SendMessageUpwards("ManipulationCanceled", eventData);
    }


}
