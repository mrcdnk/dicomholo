using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetRotation : MonoBehaviour
{
    // The object whose rotation we want to match.
    private Quaternion _target;

    // Angular speed in degrees per sec.
    public float Speed = 200f;

    public bool Reset;

    private void Awake()
    {
        _target = transform.rotation;
    }

    void Update()
    {
        if (!Reset) return;

        // The step size is equal to speed times frame time.
        var step = Speed * Time.deltaTime;

        // Rotate our transform a step closer to the target's.
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _target, step);
        if (transform.rotation == _target)
        {
            Reset = false;
        }
    }


    public void ResetToZero()
    {
        Reset = true;
    }
}
