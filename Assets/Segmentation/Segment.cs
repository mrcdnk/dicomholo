using System;
using System.Collections;
using System.Collections.Generic;
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
        private ulong[,] segmentData;

        private int width = 0, height = 0, slices = 0;

        private readonly SegmentationColor color;

        /// <summary>
        /// Creates an empty Segment with uninitialized data array.
        /// </summary>
        /// <param name="color">Color Number used inside the shader.</param>
        public Segment(SegmentationColor color)
        {
            this.color = color;
        }

        /// <summary>
        /// Allocates the data array.
        /// </summary>
        /// <param name="width"> width of a slice</param>
        /// <param name="height"> height of a slice</param>
        /// <param name="slices"> number of slices</param>
        public void Allocate(int width, int height, int slices)
        {
            this.width = width;
            this.height = height;
            this.slices = slices;

            segmentData = new ulong[slices, ((width * height) / 64) + 1];
        }

        /// <summary>
        /// Iterates over the given data, which has to match the allocated size, to check whether data points are inside the segment or not.
        /// </summary>
        /// <param name="data">Base data volume</param>
        /// <param name="parameters">Custom Parameter Object</param>
        public abstract void Fit(uint[,,] data, TP parameters);

        /// <summary>
        /// Use to check whether a given coordinate is inside the segment or not.
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="z">slice number</param>
        /// <returns>true if the given point is inside the segment.</returns>
        public bool Contains(int x, int y, int z)
        {
            int longnum = (x + y * width) >> 6;
            int bitnum = (x + y * width) % 64;

            return (segmentData[z, longnum] & (ulong)1 << bitnum) != 0;
        }

        /// <summary>
        /// Resets the Segment
        /// </summary>
        public void Clear()
        {
           Array.Clear(segmentData, 0, segmentData.Length);
        }

        public abstract bool Done();

        /// <summary>
        /// Sets the given point to the given value.
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="z">slice number</param>
        /// <param name="value">The boolean value to set the given point to.</param>
        private void Set(int x, int y, int z, bool value)
        {
            int longnum = (x + y * width) >> 6;
            int bitnum = (x + y * width) % 64;

            if (value)
            {
                segmentData[z, longnum] |= (ulong)1 << bitnum;
            }
            else
            {
                segmentData[z, longnum] &= ~(ulong)1 << bitnum;
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
            return color;
        }

        /// <summary>
        /// Returns the segment array.
        /// </summary>
        /// <returns>
        /// The segment array.
        /// </returns>
        public ulong[,] GetSegmentation()
        {
            return segmentData;
        }
    }

    /// <summary>
    /// Used by the Shader to determine which color to draw for this shader.
    /// Currently only 3 Colors are supported.
    /// </summary>
    public enum SegmentationColor : uint
    {
        RED = 1,
        BLUE = 2,
        GREEN = 3
    }

}


