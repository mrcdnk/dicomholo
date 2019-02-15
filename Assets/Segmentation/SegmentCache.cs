using DICOMParser;
using DICOMViews;
using DICOMViews.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Threads;
using UnityEngine;

namespace Segmentation
{
    public class SegmentCache : MonoBehaviour
    {
        private Segment[] _segments = new Segment[3];
        private readonly Dictionary<SliceType, Texture2D[]> _sliceSegments = new Dictionary<SliceType, Texture2D[]>(3);

        private int _width;
        private int _height;
        private int _slices;
        private bool _texturesInvalid = true;
        private bool _textureLock = false;

        private List<Texture3D> _volumeLocks = new List<Texture3D>();

        private ImageStack _imageStack;

        private readonly List<Tuple<ThreadGroupState, uint, Action<uint>>> _currentWorkloads =
            new List<Tuple<ThreadGroupState, uint, Action<uint>>>(5);

        public const uint All = 0xFFFFFFFF;
        public static readonly uint One = GetSelector(0);
        public static readonly uint Two = GetSelector(1);
        public static readonly uint Three = GetSelector(2);

        public const int MaxSegmentCount = 3;

        public SegmentTextureReady TextureReady = new SegmentTextureReady();
        public SegmentChanged SegmentChanged = new SegmentChanged();

        // Start is called before the first frame update
        void Start()
        {
            _segments[0] = new Segment(SegmentationColor.Red);
            _segments[1] = new Segment(SegmentationColor.Green);
            _segments[2] = new Segment(SegmentationColor.Blue);

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

            _width = width;
            _height = height;
            _slices = slices;

            foreach (var segment in _segments)
            {
                segment.Allocate(width, height, slices);
            }
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
                                _sliceSegments[type][index] =
                                    new Texture2D(_width, _height, TextureFormat.ARGB32, false);
                                break;
                            case SliceType.Sagittal:
                                _sliceSegments[type][index] =
                                    new Texture2D(_height, _slices, TextureFormat.ARGB32, false);
                                break;
                            case SliceType.Frontal:
                                _sliceSegments[type][index] =
                                    new Texture2D(_width, _slices, TextureFormat.ARGB32, false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

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
        public void CreateSegment<TP>(uint index, SegmentationStrategy<TP> segmentationStrategy, TP parameters)
        {
            _currentWorkloads.Add(new Tuple<ThreadGroupState, uint, Action<uint>>(
                segmentationStrategy.Fit(_segments[GetIndex(index)], _imageStack.GetData(), parameters), index, OnSegmentChange));
        }

        /// <summary>
        /// Called when a segmentation has been completed.
        /// </summary>
        /// <param name="index">selector for the created segment.</param>
        private void OnSegmentChange(uint index)
        {
            SegmentChanged.Invoke(index);
        }

        /// <summary>
        /// Coroutine used to apply the given segments to the given texture3D. Waits until previous Coroutine writing to same texture is done.
        /// </summary>
        /// <param name="texture3D">texture to write to.</param>
        /// <param name="selector">selection of segments that are going to be written to the texture.</param>
        /// <returns></returns>
        public IEnumerator ApplySegments(Texture3D texture3D, uint selector = 0xFFFFFFFF)
        {
            yield return AccessVolume(texture3D);
            for (var shift = 0; shift < MaxSegmentCount; shift++)
            {
                if (!ContainsIndex(selector, shift))
                {
                    continue;
                }

                if (_segments[shift].IsClear)
                {
                    continue;
                }

                yield return _segments[shift].WriteToTexture(texture3D);
            }

            texture3D.Apply();

            FreeVolume(texture3D);
        }

        /// <summary>
        /// Coroutine used to apply the given segments to the cached textures.
        /// </summary>
        /// <param name="selector">Selection of segments that should be written to the texture.</param>
        /// <param name="clearFlag">If set to true, the segments will be cleared before writing to them.</param>
        /// <returns></returns>
        public IEnumerator ApplyTextures(uint selector = 0xFFFFFFFF, bool clearFlag = false)
        {
            var alreadyClear = true;

            for (var index = 0; index < _segments.Length; index++)
            {
                var segment = _segments[index];

                if (segment.IsClear)
                {
                    selector = selector & ~GetSelector(index);
                }

                alreadyClear = alreadyClear && segment.IsClear;
            }

            if (!alreadyClear && clearFlag)
            {
                ClearTextures();
            }

            yield return AccessTextures();

            Texture2D currentTexture;

            for (var i = 0; i < _slices; ++i)
            {
                currentTexture = _sliceSegments[SliceType.Transversal][i];

                for (var shift = 0; shift < MaxSegmentCount; shift++)
                {
                    if (!ContainsIndex(selector, shift))
                    {
                        continue;
                    }

                    _segments[shift].WriteToTransversal(currentTexture, i);
                    yield return null;
                }

                currentTexture.Apply();
                TextureReady.Invoke(currentTexture, SliceType.Transversal, i);
            }

            yield return null;

            for (var i = 0; i < _width; ++i)
            {
                currentTexture = _sliceSegments[SliceType.Sagittal][i];

                for (var shift = 0; shift < MaxSegmentCount; shift++)
                {
                    if (!ContainsIndex(selector, shift))
                    {
                        continue;
                    }

                    _segments[shift].WriteToSagittal(currentTexture, i);
                    yield return null;
                }

                currentTexture.Apply();
                TextureReady.Invoke(currentTexture, SliceType.Sagittal, i);
            }

            yield return null;

            for (var i = 0; i < _height; ++i)
            {
                currentTexture = _sliceSegments[SliceType.Frontal][i];

                for (var shift = 0; shift < MaxSegmentCount; shift++)
                {
                    if (!ContainsIndex(selector, shift))
                    {
                        continue;
                    }

                    _segments[shift].WriteToFrontal(currentTexture, i);
                    yield return null;
                }

                currentTexture.Apply();
                TextureReady.Invoke(currentTexture, SliceType.Frontal, i);
            }

            FreeTextures();
        }

        public Texture2D GetSegmentTexture(SliceType type, int index)
        {
            return _sliceSegments[type][index];
        }

        public Segment GetSegment(int index)
        {
            return _segments[index];
        }


        /// <summary>
        /// Returns the selector for a given index
        /// </summary>
        /// <param name="index">the actual index of a segment, smaller than MaxSegmentCount.</param>
        /// <returns></returns>
        public static uint GetSelector(int index)
        {
            return (uint)1 << (31 - index);
        }

        /// <summary>
        /// Returns the segment index for the first segment in the given selector.
        /// </summary>
        /// <param name="selector">the selector for segments.</param>
        /// <returns>Actual index for the segments array.</returns>
        public static int GetIndex(uint selector)
        {
            for (int shift = 0; shift < MaxSegmentCount; shift++)
            {
                if (ContainsIndex(selector, shift))
                {
                    return shift;
                }
            }

            return -1;
        }

        /// <summary>
        /// Checks if the given selector contains the given index.
        /// </summary>
        /// <param name="selector">Segment selector</param>
        /// <param name="index">index of the segment</param>
        /// <returns>true if the bit at the index is set to 1</returns>
        public static bool ContainsIndex(uint selector, int index)
        {
            return (selector & GetSelector(index)) > 0;
        }

        private IEnumerator AccessTextures()
        {
            while (_textureLock)
            {
                yield return null;
            }

            _textureLock = true;
        }
        
        private void FreeTextures()
        {
            _textureLock = false;
        }

        private IEnumerator AccessVolume(Texture3D texture3D)
        {
            while (_volumeLocks.Contains(texture3D))
            {
                yield return null;
            }

            _volumeLocks.Add(texture3D);
        }

        private void FreeVolume(Texture3D texture3D)
        {
            _volumeLocks.Remove(texture3D);
        }

    }
}