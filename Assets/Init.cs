using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Init : MonoBehaviour {

	// Use this for initialization
	void Start () {
        if (XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale))
        {
            // RoomScale mode was set successfully.  App can now assume that y=0 in Unity world coordinate represents the floor.
        }
        else
        {
            // RoomScale mode was not set successfully.  App cannot make assumptions about where the floor plane is.
            Debug.Log("Failed to set roomscale mode");
        }

    }

    // Update is called once per frame
    void Update () {
		
	}
}
