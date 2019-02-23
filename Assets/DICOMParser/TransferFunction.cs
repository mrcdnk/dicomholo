using System;
using System.Collections.Generic;
using UnityEngine;

namespace DICOMParser
{
    /// <summary>
    /// This class contains Color32 => Color32 functions that can be applied when processing the pixels contained inside a DICOM file.
    /// </summary>
    public class TransferFunction
    {
        private TransferFunction() { }

        
        /// <summary>
        /// Control knots for the cubic interpolation used below
        /// </summary>
        private static readonly List<ControlPoint> ColorKnots = new List<ControlPoint> {
            new ControlPoint(.91f, .7f, .61f, 0),
            new ControlPoint(.91f, .7f, .61f, 80),
            new ControlPoint(1.0f, 1.0f, .85f, 82),
            new ControlPoint(1.0f, 1.0f, .85f, 256)
        };

        /// <summary>
        /// Control knots for the cubic interpolation used below
        /// </summary>
        private static readonly List<ControlPoint> AlphaKnots = new List<ControlPoint> {
            new ControlPoint(0.0f, 0),
            new ControlPoint(0.0f, 40),
            new ControlPoint(0.2f, 60),
            new ControlPoint(0.05f, 63),
            new ControlPoint(0.0f, 80),
            new ControlPoint(0.9f, 82),
            new ControlPoint(1f, 256)
        };

        private static byte GetRValue(int isoValue)
        {
            double isoPow2 = Math.Pow(isoValue, 2);
            double isoPow3 = Math.Pow(isoValue, 3);

            if (isoValue >= 0 && isoValue <= 80)
            {
                return (byte) ((0.0000034496 * isoPow3 - 0.022077 * isoValue + 0.91) * byte.MaxValue);
            }

            if (isoValue > 80 && isoValue <= 82)

            {
                return (byte) ((-0.00020269 * isoPow3 + 0.049473 * isoPow2 - 3.9799 * isoValue + 106.45) * byte.MaxValue);
            }

            return (byte) ((0.00000074373 * isoPow3 - 0.00057118 * isoPow2 + 0.12371 * isoValue - 5.7133) * byte.MaxValue);
        }

        private static byte GetGValue(int isoValue)
        {
            double isoPow2 = Math.Pow(isoValue, 2);
            double isoPow3 = Math.Pow(isoValue, 3);

            if (isoValue >= 0 && isoValue <= 80)
            {
                return (byte)((0.00001499 * isoPow3 - 0.073592 * isoValue + 0.7) * byte.MaxValue);
            }

            if (isoValue > 80 && isoValue <= 82)

            {
                return (byte)((-0.00067563 * isoPow3 + 0.16491 * isoPow2 - 13.266 * isoValue + 325.51) * byte.MaxValue);
            }

            return (byte)((0.00000074373 * isoPow3 - 0.00057118 * isoPow2 + 0.12371 * isoValue - 5.7133) * byte.MaxValue);
        }

        private static byte GetBValue(int isoValue)
        {
            double isoPow2 = Math.Pow(isoValue, 2);
            double isoPow3 = Math.Pow(isoValue, 3);

            if (isoValue >= 0 && isoValue <= 80)
            {
                return (byte)((0.0000091989 * isoPow3 - 0.058873 * isoValue + 0.61) * byte.MaxValue);
            }

            if (isoValue > 80 && isoValue <= 82)

            {
                return (byte)((-0.00054050 * isoPow3 + 0.13193 * isoPow2 - 10.613 * isoValue + 282.06) * byte.MaxValue);
            }

            return (byte)((0.0000019833 * isoPow3 - 0.0015232 * isoPow2 + 0.32988 * isoValue - 17.052) * byte.MaxValue);
        }

        private static byte GetAValue(int isoValue)
        {
            double isoPow2 = Math.Pow(isoValue, 2);
            double isoPow3 = Math.Pow(isoValue, 3);

            if (isoValue >= 0 && isoValue <= 40)
            {
                return (byte)((0.0000067176 * isoPow3 - 0.010748 * isoValue) * byte.MaxValue);
            }

            if (isoValue > 40 && isoValue <= 60)

            {
                return (byte)((-0.000069047 * isoPow3 + 0.0090918 * isoPow2 - 0.37442 * isoValue + 4.8489) * byte.MaxValue);
            }

            if (isoValue > 60 && isoValue <= 63)

            {
                return (byte)((-0.0012083 * isoPow3 + 0.21416 * isoPow2 - 12.678 * isoValue + 250.93) * byte.MaxValue);
            }

            if (isoValue > 63 && isoValue <= 80)

            {
                return (byte)((0.0011087 * isoPow3 - 0.22375 * isoPow2 - 14.910 * isoValue - 328.43) * byte.MaxValue);
            }

            if (isoValue > 80 && isoValue <= 82)

            {
                return (byte)((-0.0077376 * isoPow3 + 189.94 * isoPow2 - 154.94 * isoValue + 4200.9) * byte.MaxValue);
            }

            return (byte)((0.0000078884 * isoPow3 - 0.0060346 * isoPow2 + 1.3033 * isoValue - 69.745) * byte.MaxValue);
        }

        /// <summary>
        /// To be used for cubic interpolation as a transfer function
        /// </summary>
        private class ControlPoint
        {
            public Color32 Color;
            public int IsoValue;

            public ControlPoint(float r, float g, float b, int isoValue)
            {
                Color = new Color32((byte)(r*byte.MaxValue), (byte)(g * byte.MaxValue), (byte)(b * byte.MaxValue), 1);
                IsoValue = isoValue;
            }

            public ControlPoint(float alpha, int isoValue)
            {
                Color = new Color32(0,0,0, (byte)(alpha * byte.MaxValue));
                IsoValue = isoValue;
            }
        }

        /// <summary>
        /// Applies static Transfer function for skin and bones.
        /// </summary>
        /// <param name="isoColor"></param>
        /// <returns></returns>
        public static Color32 SKIN_BONES(Color32 isoColor)
        {
            var isoValue = isoColor.r;
            isoColor.r = GetRValue(isoValue);
            isoColor.g = GetGValue(isoValue);
            isoColor.b = GetBValue(isoValue);
            isoColor.a = GetAValue(isoValue);
            return isoColor;
        }

        /// <summary>
        /// Dynamic alpha calculation based on grey rgb(x,x,x) value, where x is the intensity.
        /// </summary>
        /// <param name="isoColor">input color (assumed r=g=b)</param>
        /// <returns>input color with calculated alpha value</returns>
        public static Color32 DYN_ALPHA(Color32 isoColor)
        {
            double dynAlpha = 200 * (Math.Max(isoColor.r - 5, 0)) / 255d;
            isoColor.a = (byte)dynAlpha;
            return isoColor;
        }

        /// <summary>
        /// Identity function color32 -> color32 
        /// </summary>
        /// <param name="isoColor"> input color </param>
        /// <returns>unchanged input color</returns>
        public static Color32 Identity(Color32 isoColor)
        {
            return isoColor;
        }




    }

}
