
namespace DICOMViews
{
    /// <summary>
    /// SliceType Describes from which perspective the slice is generated
    /// </summary>
    public enum SliceType
    {
        Transversal, // The images as they are stored inside the DICOM File
        Sagittal, // Side view
        Frontal // View from the Front
    }
}

