using System;
using DICOMViews;
using Threads;
using UnityEngine;

namespace Segmentation
{
    /// <summary>
    /// Contains an Array of arrays with a 1D Array for each slice of the volume.
    /// Implemented for speed & low mem usage
    /// </summary>
    /// <typeparam name="TP">Customizable type for the Parameter Object to pass any parameters to the Fit function</typeparam>
    public abstract class Segment<TP>
    {
        private ulong[,] _segmentData;

        public SegmentationColor SegmentColor { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int Slices { get; set; }

        /// <summary>
        /// Creates an empty Segment with uninitialized data array.
        /// </summary>
        /// <param name="segmentColor">Color Number used inside the shader.</param>
        public Segment(SegmentationColor segmentColor)
        {
            SegmentColor = segmentColor;
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

            _segmentData = new ulong[slices, (width * height) / 64 + 1];
        }

        /// <summary>
        /// Iterates over the given data, which has to match the allocated size, to check whether data points are inside the segment or not.
        /// </summary>
        /// <param name="data">Base data volume</param>
        /// <param name="parameters">Custom Parameter Object</param>
        /// <returns>The ThreadGroupState to enable progress monitoring and callback on finish.</returns>
        public abstract ThreadGroupState Fit(int[] data, TP parameters);

        /// <summary>
        /// Use to check whether a given coordinate is inside the segment or not.
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="z">slice number</param>
        /// <returns>true if the given point is inside the segment.</returns>
        public bool Contains(int x, int y, int z)
        {
            int longnum = (x + y * Width) >> 6;
            int bitnum = (x + y * Width) % 64;

            return (_segmentData[z, longnum] & (ulong)1 << bitnum) != 0;
        }

        /// <summary>
        /// Resets the Segment
        /// </summary>
        public void Clear()
        {
           Array.Clear(_segmentData, 0, _segmentData.Length);
        }

        /// <summary>
        /// Returns true when there is no work left to do, only needs to be overridden when using multiple threads.
        /// </summary>
        /// <returns></returns>
        public abstract bool Done();

        /// <summary>
        /// Sets the given point to the given value.
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="z">slice number</param>
        /// <param name="value">The boolean value to set the given point to.</param>
        protected void Set(int x, int y, int z, bool value)
        {
            var longnum = (x + y * Width) >> 6;
            var bitnum = (x + y * Width) % 64;

            if (value)
            {
                _segmentData[z, longnum] |= (ulong)1 << bitnum;
            }
            else
            {
                _segmentData[z, longnum] &= ~(ulong)1 << bitnum;
            }
        }

        /// <summary>
        /// Use to check which color number will be used for this segment.
        /// </summary>
        /// <returns>
        /// The SegmentationColor of this segment.
        /// </returns>
        public SegmentationColor GetColor()
        {
            return SegmentColor;
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
                    break;
                case SliceType.Sagittal:
                    break;
                case SliceType.Frontal:
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
            var colors = texture2D.GetPixels();

            for (var y = 0; y < Height; ++y)
            {
                for (var x = 0; x < Width; ++x)
                {
                    var index = y * Width + x;

                    colors[index] = GetColor(x, y, id);
                }
            }
        }

        /// <summary>
        /// Writes the segment into a texture 3D.
        /// </summary>
        /// <param name="texture3D">The target texture 3D to override</param>
        public void WriteToTexture(Texture3D texture3D)
        {
            var pixelColors = texture3D.GetPixels();

            var index = 0;

            for (var z = 0; z < Slices; z++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        pixelColors[index] = GetColor(x,y,z);
                        index++;
                    }
                    index++;
                }
                index++;
            }
        }

        /// <summary>
        /// Returns the unity color representation of the given pixel
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="z">index of the dicom image</param>
        /// <returns></returns>
        public Color GetColor(int x, int y, int z)
        {
            if (Contains(x, y, z))
            {
               return GetColor(SegmentColor);
            }
            else
            {
                return Color.clear;
            }
        }

        /// <summary>
        /// Converts the given Segmentation Color to the UnityColor to use.
        /// </summary>
        /// <param name="color">Segmentation color enum</param>
        /// <returns>Unity color representation</returns>
        public static Color GetColor(SegmentationColor color)
        {
            switch (color)
            {
                case SegmentationColor.Red:
                    return Color.red;
                case SegmentationColor.Blue:
                    return Color.blue;
                case SegmentationColor.Green:
                    return Color.green;
                default:
                    return Color.clear;
            }
        }
    }

    /// <summary>
    /// Used by the Shader to determine which color to draw for this shader.
    /// Currently only 3 Colors are supported.
    /// </summary>
    public enum SegmentationColor : uint
    {
        Red = 1,
        Blue = 2,
        Green = 3
    }

}


