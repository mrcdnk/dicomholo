﻿using System;
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
        const int SegmentTransparency = 180;

        private ulong[,] _segmentData;
        private object[] _locks;

        private bool _isClear = true;

        internal readonly ThreadGroupState _currentWorkload;

        public SegmentationColor SegmentColor { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Slices { get; private set; }
        public bool IsClear => _isClear;

        /// <summary>
        /// Creates an empty Segment with uninitialized data array.
        /// </summary>
        /// <param name="segmentColor">Color Number used inside the shader.</param>
        public Segment(SegmentationColor segmentColor)
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
            _isClear = true;
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
                _isClear = false;

                lock (_locks[z])
                {
                    _segmentData[z, longnum] |= (ulong)1 << bitnum;
                }
            }
            else
            {
                lock (_locks[z])
                {
                    _segmentData[z, longnum] &= ~((ulong)1 << bitnum);
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
               return GetColor(SegmentColor);
            }
            else
            {
                return Color.clear;
            }
        }

        public Color32 GetColor()
        {
            return GetColor(SegmentColor);
        }

        /// <summary>
        /// Combines multiple colors to the same
        /// </summary>
        /// <param name="target"></param>
        /// <param name="colorToAdd"></param>
        public static Color32 AddColor(Color32 target, SegmentationColor colorToAdd)
        {
            switch (colorToAdd)
            {
                case SegmentationColor.Red:
                    target.r = 255;
                    break;
                case SegmentationColor.Blue:
                    target.b = 255;
                    break;
                case SegmentationColor.Green:
                    target.g = 255;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return target;
        }

        private Color32 OverlapColors(Color32 c1, Color32 c2)
        {
            Color32 color = new Color32();

            color.a = (byte) ((1 - c1.a) * c2.a + c1.a);

            if (color.a == 0)
            {
                return c1;
            }

            color.r = (byte) (((1 - c1.a) * c2.a * c2.r + c1.a * c1.r) / color.a);
            color.g = (byte) (((1 - c1.a) * c2.a * c2.g + c1.a * c1.g) / color.a);
            color.b = (byte) (((1 - c1.a) * c2.a * c2.b + c1.a * c1.b) / color.a);

            return color;
        }

        /// <summary>
        /// Converts the given Segmentation Color to the UnityColor to use.
        /// </summary>
        /// <param name="color">Segmentation color enum</param>
        /// <returns>Unity color representation</returns>
        public static Color32 GetColor(SegmentationColor color)
        {
            switch (color)
            {
                case SegmentationColor.Red:
                    return new Color32(255, 0, 0, SegmentTransparency);
                case SegmentationColor.Blue:
                    return new Color32(0, 0, 255, SegmentTransparency); ;
                case SegmentationColor.Green:
                    return new Color32(0, 255, 0, SegmentTransparency); ;
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


