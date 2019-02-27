using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynchronizeRotationTo : MonoBehaviour
{
    public Transform RotationSource;

    // Update is called once per frame
    private void Update()
    {
        // The step size is equal to speed times frame time.

        // Rotate our transform a step closer to the target's.
        transform.rotation = RotationSource.rotation;
    }
}
