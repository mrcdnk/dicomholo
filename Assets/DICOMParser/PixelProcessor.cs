using System;
using UnityEngine;

namespace DICOMParser
{
    /// <summary>
    /// This class contains Color32 => Color32 functions that can be applied when processing the pixels contained inside a DICOM file.
    /// </summary>
    public class PixelProcessor
    {
        private PixelProcessor() { }

        /// <summary>
        /// Dynamic alpha calculation based on grey rgb(x,x,x) value, where x is the intensity.
        /// </summary>
        /// <param name="argb">input color (assumed r=g=b)</param>
        /// <returns>input color with calculated alpha value</returns>
        public static Color32 DYN_ALPHA(Color32 argb)
        {
            double dynAlpha = 210 * (Math.Max(argb.r - 10, 0)) / 255d;
            argb.a = (byte)dynAlpha;
            return argb;
        }

        /// <summary>
        /// Identity function color32 -> color32 
        /// </summary>
        /// <param name="argb"> input color </param>
        /// <returns>unchanged input color</returns>
        public static Color32 Identity(Color32 argb)
        {
            return argb;
        }

    }

}
