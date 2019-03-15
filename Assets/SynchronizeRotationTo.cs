using UnityEngine;

/// <summary>
/// Synchronizes the objects global rotation to the given source rotation
/// </summary>
public class SynchronizeRotationTo : MonoBehaviour
{
    public Transform RotationSource;

    private bool _resetAllPossibleChildren = true;

    // Update is called once per frame
    private void Update()
    {
        transform.rotation = RotationSource.rotation;

        if (!_resetAllPossibleChildren) return;

        //Fix orientation of specifically the rotation object

        foreach (var componentsInChild in GetComponentsInChildren<ResetRotation>())
        {
            componentsInChild.ResetToZero();
        }

        _resetAllPossibleChildren = false;
    }
}
