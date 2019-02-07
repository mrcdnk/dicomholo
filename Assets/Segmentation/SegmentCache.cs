﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DICOMParser;
using DICOMViews;
using DICOMViews.Events;
using Threads;
using UnityEngine;

namespace Segmentation
{
    public class SegmentCache : MonoBehaviour
    {
        private Segment[] _segments = new Segment[3];
        private readonly Dictionary<SliceType, Texture2D[]> _sliceSegments = new Dictionary<SliceType, Texture2D[]>(3);
        private Texture3D _volumeSegments;

        private int _widthV;
        private int _heightV;
        private int _slicesV;
        private int _widthT;
        private int _heightT;
        private int _slicesT;
        private ImageStack _imageStack;

        private readonly List<Tuple<ThreadGroupState, int, Action<int>>> _currentWorkloads = new List<Tuple<ThreadGroupState, int, Action<int>>>(5);

        public SegmentVolumeReady VolumeReady = new SegmentVolumeReady();
        public SegmentTextureReady TextureReady = new SegmentTextureReady();

        // Start is called before the first frame update
        void Start()
        {
            _segments[0] = new Segment(SegmentationColor.Red);
            _segments[1] = new Segment(SegmentationColor.Blue);
            _segments[2] = new Segment(SegmentationColor.Green);

            _imageStack = FindObjectOfType<ImageStack>();
        }

        // Update is called once per frame
        void Update()
        {
            int index = 0;

            while (_currentWorkloads.Count > 0 && index < _currentWorkloads.Count)
            {
                var tuple = _currentWorkloads[index];
                if (tuple.Item1.Progress == tuple.Item1.TotalProgress && tuple.Item1.Working == 0)
                {
                    //Remove from list
                    RemoveWorkload(index);
                    break;
                }
                index++;
            }
        }

        private void RemoveWorkload(int index)
        {
            var tuple = _currentWorkloads[index];
            tuple.Item3.Invoke(tuple.Item2);
          
            _currentWorkloads.RemoveAt(index);
        }

        /// <summary>
        /// Creates a new Texture3D if necessary and initializes its color to clear.
        /// </summary>
        /// <param name="width">width of a slice</param>
        /// <param name="height">height of a slice</param>
        /// <param name="slices">number of slices</param>
        public void InitializeVolume(int width, int height, int slices)
        {
            var anyChanges = width != _widthV || height != _heightV || slices != _slicesV;

            _widthV = width;
            _heightV = height;
            _slicesV = slices;

            if (anyChanges)
            {
                Destroy(_volumeSegments);
                _volumeSegments = new Texture3D(width, height, slices, TextureFormat.RGB24, true);
            }

            var pixels = new Color[width*height*slices];

            _volumeSegments.SetPixels(pixels);
            _volumeSegments.Apply();
        }

        /// <summary>
        /// Creates new Textures if necessary and initializes their colors to clear.
        /// </summary>
        /// <param name="width">width of a slice</param>
        /// <param name="height">height of a slice</param>
        /// <param name="slices">number of slices</param>
        public void InitializeTextures(int width, int height, int slices)
        { 
            var anyChanges = width != _widthT || height != _heightT || slices != _slicesT;

            _widthT = width;
            _heightT = height;
            _slicesT = slices;

            if (anyChanges)
            {
                _sliceSegments[SliceType.Transversal] = new Texture2D[slices];
                _sliceSegments[SliceType.Frontal] = new Texture2D[height];
                _sliceSegments[SliceType.Sagittal] = new Texture2D[width];

                foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
                {
                    for (var index = 0; index < _sliceSegments[type].Length; index++)
                    {
                        var texture2D = _sliceSegments[type][index];
                        Destroy(texture2D);

                        switch (type)
                        {
                            case SliceType.Transversal:
                                _sliceSegments[type][index] = new Texture2D(width, height, TextureFormat.ARGB32, false);
                                break;
                            case SliceType.Sagittal:
                                _sliceSegments[type][index] = new Texture2D(height, slices, TextureFormat.ARGB32, false);
                                break;
                            case SliceType.Frontal:
                                _sliceSegments[type][index] = new Texture2D(width, slices, TextureFormat.ARGB32, false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                for (var index = 0; index < _sliceSegments[type].Length; index++)
                {
                    switch (type)
                    {
                        case SliceType.Transversal:
                            _sliceSegments[type][index].SetPixels(new Color[width*height]);
                            break;
                        case SliceType.Sagittal:
                            _sliceSegments[type][index].SetPixels(new Color[height*slices]);
                            break;
                        case SliceType.Frontal:
                            _sliceSegments[type][index].SetPixels(new Color[width*height]);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        /// <summary>
        /// Starts creating a Segmentation
        /// </summary>
        /// <typeparam name="TP">The type of the segmentation parameters</typeparam>
        /// <param name="index">Index of the segment to create, range ist limited from 0 to 2</param>
        /// <param name="segmentationStrategy">Strategy for creating a Segmentation</param>
        /// <param name="parameters">Parameters used by the segmentation strategy</param>
        public void CreateSegment<TP>(int index, SegmentationStrategy<TP> segmentationStrategy, TP parameters)
        {
            _currentWorkloads.Add(new Tuple<ThreadGroupState, int, Action<int>>(segmentationStrategy.Fit(_segments[index], _imageStack.GetData(), parameters), index, OnSegmentCreated));
        }

        private void OnSegmentCreated(int index)
        {

        }

        private void ApplyVolume(int index)
        {

        }

        private IEnumerator ApplyTextures(int index)
        {
            yield return null;
        }

        public Texture2D GetSegment(SliceType type, int index)
        {
            return _sliceSegments[type][index];
        }

        public Texture3D GetVolume()
        {
            return _volumeSegments;
        }
    }
}
