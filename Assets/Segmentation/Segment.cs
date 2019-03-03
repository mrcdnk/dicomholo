using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DICOMViews;
using Threads;
using UnityEngine;

namespace Segmentation
{
    /// <summary>
    /// Contains an Array of arrays with a 1D Array for each slice of the volume.
    /// Implemented for speed & low mem usage
    /// </summary>
    public class Segment
    {
        public const int SegmentTransparency = 180;

        private ulong[,] _segmentData;
        private object[] _locks;
        internal readonly ThreadGroupState _currentWorkload;

        public Color SegmentColor { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Slices { get; private set; }
        public bool IsClear { get; private set; } = true;

        /// <summary>
        /// Creates an empty Segment with uninitialized data array.
        /// </summary>
        /// <param name="segmentColor">Color Number used inside the shader.</param>
        public Segment(Color segmentColor)
        {
            SegmentColor = segmentColor;
            _currentWorkload = new ThreadGroupState();
        }

        /// <summary>
        /// Allocates the data array.
        /// </summary>
        /// <param name="width"> width of a slice</param>
        /// <param name="height"> height of a slice</param>
        /// <param name="slices"> number of slices</param>
        public void Allocate(int width, int height, int slices)
        {
            Width = width;
            Height = height;
            Slices = slices;

            _segmentData = new ulong[slices, ((width * height) / 64) + 1];
            _locks = new object[slices];

            for (var index = 0; index < _locks.Length; index++)
            {
                _locks[index] = new object();
            }

            Clear();
        }

        /// <summary>
        /// Use to check whether a given coordinate is inside the segment or not.
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="z">slice number</param>
        /// <returns>true if the given point is inside the segment.</returns>
        public bool Contains(int x, int y, int z)
        {
            var longnum = (x + y * Width) >> 6;
            var bitnum = (x + y * Width) % 64;

            return (_segmentData[z, longnum] & (ulong) 1 << bitnum) != 0;
        }

        /// <summary>
        /// Resets the Segment
        /// </summary>
        public void Clear()
        {
            Array.Clear(_segmentData, 0, _segmentData.Length);
            IsClear = true;
        }

        /// <summary>
        /// Returns true when there is no work left to do, only needs to be overridden when using multiple threads.
        /// </summary>
        /// <returns></returns>
        public bool Done()
        {
            return _currentWorkload.Working == 0;
        }

        /// <summary>
        /// Sets the given point to the given value.
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="z">slice number</param>
        /// <param name="value">The boolean value to set the given point to.</param>
        internal void Set(int x, int y, int z, bool value = true)
        {
            var longnum = (x + y * Width) >> 6;
            var bitnum = (x + y * Width) % 64;

            if (value)
            {
                IsClear = false;

                lock (_locks[z])
                {
                    _segmentData[z, longnum] |= (ulong) 1 << bitnum;
                }
            }
            else
            {
                lock (_locks[z])
                {
                    _segmentData[z, longnum] &= ~((ulong) 1 << bitnum);
                }
            }
        }

        /// <summary>
        /// Returns the segment array.
        /// </summary>
        /// <returns>
        /// The segment array.
        /// </returns>
        public ulong[,] GetSegmentation()
        {
            return _segmentData;
        }

        /// <summary>
        /// Writes segmentation data of the slice type into the given texture.
        /// </summary>
        /// <param name="texture2D">target texture2D</param>
        /// <param name="sliceType">type of slice to be displayed</param>
        /// <param name="index">index of the slice</param>
        public void WriteToTexture(Texture2D texture2D, SliceType sliceType, int index)
        {
            switch (sliceType)
            {
                case SliceType.Transversal:
                    WriteToTransversal(texture2D, index);
                    break;
                case SliceType.Sagittal:
                    WriteToSagittal(texture2D, index);
                    break;
                case SliceType.Frontal:
                    WriteToFrontal(texture2D, index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sliceType), sliceType, "Invalid SliceType");
            }
        }

        /// <summary>
        /// Writes segmentation data into a transversal 2D texture
        /// </summary>
        /// <param name="texture2D">target Texture 2D</param>
        /// <param name="id">the id of the slice</param>
        public void WriteToTransversal(Texture2D texture2D, int id)
        {
            var colors = texture2D.GetPixels32();

            for (var y = 0; y < Height; ++y)
            {
                for (var x = 0; x < Width; ++x)
                {
                    var index = y * Width + x;

                    colors[index] = OverlapColors(colors[index], GetColor(x, y, id));
                }
            }

            texture2D.SetPixels32(colors);
            texture2D.Apply();
        }

        /// <summary>
        /// Writes segmentation data into a sagittal 2D texture
        /// </summary>
        /// <param name="texture2D">target Texture 2D</param>
        /// <param name="id">the id of the slice</param>
        public void WriteToSagittal(Texture2D texture2D, int id)
        {
            var colors = texture2D.GetPixels32();

            for (var z = 0; z < Slices; ++z)
            {
                for (var y = 0; y < Height; ++y)
                {
                    var index = z * Width + y;

                    Color32 color = GetColor(id, y, z);

                    if (color != Color.clear)
                    {
                        colors[index] = OverlapColors(colors[index], GetColor(id, y, z));
                    }
                }
            }

            texture2D.SetPixels32(colors);
            texture2D.Apply();
        }

        /// <summary>
        /// Writes segmentation data into a frontal 2D texture
        /// </summary>
        /// <param name="texture2D">target Texture 2D</param>
        /// <param name="id">the id of the slice</param>
        public void WriteToFrontal(Texture2D texture2D, int id)
        {
            var colors = texture2D.GetPixels32();

            for (var z = 0; z < Slices; ++z)
            {
                for (var x = 0; x < Width; ++x)
                {
                    var index = z * Height + x;

                    colors[index] = OverlapColors(colors[index], GetColor(x, id, z));
                }
            }

            texture2D.SetPixels32(colors);
            texture2D.Apply();
        }

        /// <summary>
        /// Returns the unity color representation of the given pixel
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="z">index of the dicom image</param>
        /// <returns></returns>
        public Color32 GetColor(int x, int y, int z)
        {
            if (Contains(x, y, z))
            {
                return SegmentColor;
            }
            else
            {
                return Color.clear;
            }
        }

        /// <summary>
        /// Adds a 30% tint to the target color based on the given tintColor without affecting alpha or intensity.
        /// </summary>
        /// <param name="target">color that is written to</param>
        /// <param name="tintColor">the color to use for tinting</param>
        public static Color32 TintVolume(Color32 target, Color32 tintColor)
        {
            const float tintA = 0.3f;
            const float tA = 1 - tintA;

            target.r = (byte) (target.r * tA + tintA * tintColor.r);
            target.g = (byte) (target.g * tA + tintA * tintColor.g);
            target.b = (byte) (target.b * tA + tintA * tintColor.b);

            return target;
        }


        /// <summary>
        /// Correctly layers the second color over the first.
        /// </summary>
        /// <param name="c1">base color</param>
        /// <param name="c2">color to overlap on top of the base color</param>
        /// <returns></returns>
        private static Color32 OverlapColors(Color32 c1, Color32 c2)
        {
            var color = new Color32();

            var a1 = c1.a / (float)byte.MaxValue;
            var a2 = c2.a / (float)byte.MaxValue;

            color.a = (byte) ((1f - a1) * c2.a + c1.a);

            if (color.a == 0)
            {
                return c1;
            }

            color.r = (byte) ((1f - a1) * a2 * c2.r + a1 * c1.r);
            color.g = (byte) ((1f - a1) * a2 * c2.g + a1 * c1.g);
            color.b = (byte) ((1f - a1) * a2 * c2.b + a1 * c1.b);

            return color;
        }

    }
}


