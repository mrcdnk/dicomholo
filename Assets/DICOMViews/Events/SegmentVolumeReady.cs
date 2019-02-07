using UnityEngine;
using UnityEngine.Events;

namespace DICOMViews.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Indicates that the given Texture3D is ready to be displayed.
    /// </summary>
    public class SegmentVolumeReady : UnityEvent<Texture3D>
    {
       
    }
}
