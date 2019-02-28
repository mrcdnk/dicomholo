using UnityEngine;

/// <summary>
/// Synchronizes the objects global rotation to the given source rotation
/// </summary>
public class SynchronizeRotationTo : MonoBehaviour
{
    public Transform RotationSource;

    // Update is called once per frame
    private void Update()
    {
        transform.rotation = RotationSource.rotation;
    }
}
