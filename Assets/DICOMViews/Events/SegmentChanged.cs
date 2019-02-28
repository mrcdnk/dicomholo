using UnityEngine.Events;

namespace DICOMViews.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Indicates that the content of the segment has changed.
    /// </summary>
    public class SegmentChanged : UnityEvent<uint>
    {
   
    }
}
