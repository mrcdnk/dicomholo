using UnityEngine.Events;

namespace DICOMViews.Events
{
    /// <summary>
    /// Event for handling clicks on a texture. Provides percentage on x and y axis.
    /// </summary>
    public class PixelClicked : UnityEvent<float, float> { }
}
