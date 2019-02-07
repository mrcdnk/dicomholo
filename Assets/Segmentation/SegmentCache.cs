using DICOMParser;
using DICOMViews;
using DICOMViews.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threads;
using UnityEngine;

namespace Segmentation
{
    public class SegmentCache : MonoBehaviour
    {
        private Segment[] _segments = new Segment[3];
        private readonly Dictionary<SliceType, Texture2D[]> _sliceSegments = new Dictionary<SliceType, Texture2D[]>(3);
        private Texture3D _volumeSegments;

        private int _width;
        private int _height;
        private int _slices;
        private bool _texturesInvalid = true;
        private bool _volumeInvalid = true;
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
        /// Initializes the segments and size of the volume
        /// </summary>
        /// <param name="width">width of a slice</param>
        /// <param name="height">height of a slice</param>
        /// <param name="slices">number of slices</param>
        public void InitializeSize(int width, int height, int slices)
        {
            var anyChanges = width != _width || height != _height || slices != _slices;

            _texturesInvalid = anyChanges;
            _volumeInvalid = anyChanges;

            _width = width;
            _height = height;
            _slices = slices;

            foreach (var segment in _segments)
            {
                segment.Allocate(width, height, slices);
            }
        }

        /// <summary>
        /// Creates a new Texture3D if necessary and initializes its color to clear.
        /// </summary>
        public void InitializeVolume()
        {

            if (_volumeInvalid)
            {
                Destroy(_volumeSegments);
                _volumeSegments = new Texture3D(_width, _height, _slices, TextureFormat.RGB24, true);
            }

            var pixels = new Color32[_width*_height*_slices];

            _volumeSegments.SetPixels32(pixels);
            _volumeSegments.Apply();

            _volumeInvalid = false;

            VolumeReady.Invoke(_volumeSegments);
        }

        /// <summary>
        /// Creates new Textures if necessary and initializes their colors to clear.
        /// </summary>
        public void InitializeTextures()
        { 
            if (_texturesInvalid)
            {
                _sliceSegments[SliceType.Transversal] = new Texture2D[_slices];
                _sliceSegments[SliceType.Frontal] = new Texture2D[_height];
                _sliceSegments[SliceType.Sagittal] = new Texture2D[_width];

                foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
                {
                    for (var index = 0; index < _sliceSegments[type].Length; index++)
                    {
                        var texture2D = _sliceSegments[type][index];
                        Destroy(texture2D);

                        switch (type)
                        {
                            case SliceType.Transversal:
                                _sliceSegments[type][index] = new Texture2D(_width, _height, TextureFormat.ARGB32, false);
                                break;
                            case SliceType.Sagittal:
                                _sliceSegments[type][index] = new Texture2D(_height, _slices, TextureFormat.ARGB32, false);
                                break;
                            case SliceType.Frontal:
                                _sliceSegments[type][index] = new Texture2D(_width, _slices, TextureFormat.ARGB32, false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

            }

            ClearTextures();
        }

        private void ClearTextures()
        {
            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                for (var index = 0; index < _sliceSegments[type].Length; index++)
                {
                    var current = _sliceSegments[type][index];

                    current.SetPixels32(new Color32[current.GetPixels32().Length]);

                    current.Apply();

                    TextureReady.Invoke(current, type, index);
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
            Debug.Log("Created Segment: "+index);
            ApplyVolume(index);
            StartCoroutine(ApplyTextures(index));
        }

        private void ApplyVolume(int index)
        {
           _segments[index].WriteToTexture(_volumeSegments);
            VolumeReady.Invoke(_volumeSegments);
        }

        private IEnumerator ApplyTextures(int index)
        {
            for (var i = 0; i < _slices; ++i)
            {
                _segments[index].WriteToTransversal(_sliceSegments[SliceType.Transversal][i], i);
                _sliceSegments[SliceType.Transversal][i].Apply();
                TextureReady.Invoke(_sliceSegments[SliceType.Transversal][i], SliceType.Transversal, i);
            }

            yield return null;

            for (var i = 0; i < _width; ++i)
            {
                _segments[index].WriteToFrontal(_sliceSegments[SliceType.Sagittal][i], i);
                _sliceSegments[SliceType.Sagittal][i].Apply();
                TextureReady.Invoke(_sliceSegments[SliceType.Sagittal][i], SliceType.Sagittal, i);
            }

            yield return null;

            for (var i = 0; i < _height; ++i)
            {
                _segments[index].WriteToFrontal(_sliceSegments[SliceType.Frontal][i], i);
                _sliceSegments[SliceType.Frontal][i].Apply();
                TextureReady.Invoke(_sliceSegments[SliceType.Frontal][i], SliceType.Frontal, i);
            }
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
