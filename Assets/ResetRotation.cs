using UnityEngine;

/// <summary>
/// Resets the Objects rotation to its initial global rotation
/// </summary>
public class ResetRotation : MonoBehaviour
{

    // Angular speed in degrees per sec.
    public float Speed = 200f;

    public bool Reset;

    // The object whose rotation we want to match.
    private Quaternion _target;

    private void Awake()
    {
        _target = transform.rotation;
    }

    private void Update()
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

    /// <summary>
    /// Call to begin resetting the rotation
    /// </summary>
    public void ResetToZero()
    {
        Reset = true;
    }
}
