using UnityEngine.Events;

namespace DICOMViews.Events
{
    /// <summary>
    /// Event for selection of a point inside the volume by the user
    /// </summary>
    public class PointSelected : UnityEvent<int, int, int> { }
}

