using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DICOMViews.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Events indicating that the window settings have been changed in the window settings panel
    /// </summary>
    public class WindowSettingsChanged : UnityEvent<double, double>
    {

    }
}
