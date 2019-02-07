using UnityEngine.Events;

namespace DICOMViews.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Indicates that a texture is ready to be displayed.
    /// (Type of Slice, Index of Slice)
    /// </summary>
    public class SegmentTextureReady : UnityEvent<SliceType, int>
    {
    
    }
}
