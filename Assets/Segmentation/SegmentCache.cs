using DICOMParser;
using DICOMViews;
using DICOMViews.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionsMethods;
using Threads;
using UnityEngine;

namespace Segmentation
{
    /// <summary>
    /// Caches all segments and 2D Textures generated from them.
    /// </summary>
    public class SegmentCache : MonoBehaviour
    {
        private GlobalWorkIndicator _workIndicator;
        private readonly Segment[] _segments = new Segment[3];
        private readonly Dictionary<SliceType, Texture2D[]> _sliceSegments = new Dictionary<SliceType, Texture2D[]>(3);

        private int _width;
        private int _height;
        private int _slices;
        private bool _texturesInvalid = true;
        private bool _textureLock = false;

        private ImageStack _imageStack;

        private readonly List<Texture3D> _volumeLocks = new List<Texture3D>();

        private readonly List<Tuple<ThreadGroupState, uint, Action<uint>>> _currentWorkloads =
            new List<Tuple<ThreadGroupState, uint, Action<uint>>>(5);

        public const uint All = 0xFFFFFFFF;
        public const int MaxSegmentCount = 3;
        public const byte HiddenAlpha = 5;

        public static readonly uint One = GetSelector(0);
        public static readonly uint Two = GetSelector(1);
        public static readonly uint Three = GetSelector(2);    

        public SegmentTextureReady TextureReady = new SegmentTextureReady();
        public SegmentChanged SegmentChanged = new SegmentChanged();

        private void Awake()
        {
            _segments[0] = new Segment(new Color32(255, 0, 0, Segment.SegmentTransparency));
            _segments[1] = new Segment(new Color32(0, 255, 0, Segment.SegmentTransparency));
            _segments[2] = new Segment(new Color32(0, 0, 255, Segment.SegmentTransparency));
        }

        // Start is called before the first frame update
        private void Start()
        {
            _workIndicator = FindObjectOfType<GlobalWorkIndicator>();

            _imageStack = FindObjectOfType<ImageStack>();
        }

        // Update is called once per frame
        private void Update()
        {
            var index = 0;

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

        /// <summary>
        /// Removes the Workload at the given index and invokes the callback.
        /// </summary>
        /// <param name="index"></param>
        private void RemoveWorkload(int index)
        {
            var tuple = _currentWorkloads[index];
            tuple.Item3.Invoke(tuple.Item2);

            _currentWorkloads.RemoveAt(index);
            _workIndicator.FinishedWork();
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
            if (!_texturesInvalid) return;

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

            StartCoroutine(ClearTextures());

        }

        /// <summary>
        /// Clears all textures stored in the cache.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ClearTextures()
        {
            yield return AccessTextures();
            foreach (var type in Enum.GetValues(typeof(SliceType)).Cast<SliceType>())
            {
                for (var index = 0; index < _sliceSegments[type].Length; index++)
                {
                    var current = _sliceSegments[type][index];

                    current.SetPixels32(new Color32[current.GetPixels32().Length]);
                    current.Apply();

                    TextureReady.Invoke(current, type, index);
                }

                yield return null;
            }
            FreeTextures();
        }

        /// <summary>
        /// Starts creating a Segmentation
        /// </summary>
        /// <typeparam name="TP">The type of the segmentation parameters</typeparam>
        /// <param name="index">Index of the segment to create, range ist limited from 0 to 2</param>
        /// <param name="segmentationStrategy">Strategy for creating a Segmentation</param>
        /// <param name="parameters">Parameters used by the segmentation strategy</param>
        /// <param name="clearSegment">controls whether the segment will be cleared before creating the segment, defaults to true</param>
        public void CreateSegment<TP>(uint index, SegmentationStrategy<TP> segmentationStrategy, TP parameters, bool clearSegment = true)
        {
            if (clearSegment)
            {
                _segments[GetIndex(index)].Clear();
            }

            _currentWorkloads.Add(new Tuple<ThreadGroupState, uint, Action<uint>>(
                segmentationStrategy.Fit(_segments[GetIndex(index)], _imageStack.GetData(), parameters), index, OnSegmentChange));
            _workIndicator.StartedWork();
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
        /// <param name="hideBaseData">Set to true, to reduce alpha value of not contained pixels.</param>
        /// <returns></returns>
        public IEnumerator ApplySegments(Texture3D texture3D, uint selector = 0xFFFFFFFF, bool hideBaseData = false)
        {
            _workIndicator.StartedWork();
            yield return AccessVolume(texture3D);

            var pixelColors = texture3D.GetPixels32();

            var idx = 0;

            for (var z = 0; z < _slices; z++)
            {
                for (var y = 0; y < _height; y++)
                {
                    for (var x = 0; x < _width; x++, ++idx)
                    {
                        var inAny = false;

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

                            if (!_segments[shift].Contains(x, y, z)) continue;

                            pixelColors[idx] = Segment.TintVolume(pixelColors[idx], _segments[shift].SegmentColor);

                            if (hideBaseData)
                            {
                                pixelColors[idx].a = (byte) Math.Min(pixelColors[idx].a*1.05, 255);
                            }

                            inAny = true;
                        }

                        if (!inAny && hideBaseData)
                        {
                            pixelColors[idx].a = HiddenAlpha;
                        }
                    }
                }

                yield return null;
            }
            texture3D.SetPixels32(pixelColors);
            texture3D.Apply();
           
            FreeVolume(texture3D);
            _workIndicator.FinishedWork();
        }

        /// <summary>
        /// Coroutine used to apply the given segments to the cached textures.
        /// </summary>
        /// <param name="selector">Selection of segments that should be written to the texture.</param>
        /// <param name="clearFlag">If set to true, the segments will be cleared before writing to them.</param>
        /// <returns></returns>
        public IEnumerator ApplyTextures(uint selector = 0xFFFFFFFF, bool clearFlag = false)
        {
            if (!_sliceSegments.ContainsKey(SliceType.Transversal) || !_sliceSegments.ContainsKey(SliceType.Frontal) ||
                !_sliceSegments.ContainsKey(SliceType.Sagittal))
            {
                yield break;
            }

            _workIndicator.StartedWork();
            for (var index = 0; index < _segments.Length; index++)
            {
                var segment = _segments[index];

                if (segment.IsClear)
                {
                    selector = selector & ~GetSelector(index);
                }
            }

            if (clearFlag)
            {
                yield return ClearTextures();
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
                }
                yield return null;

                currentTexture.Apply();
                TextureReady.Invoke(currentTexture, SliceType.Transversal, i);
            }

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
                }
                yield return null;

                currentTexture.Apply();
                TextureReady.Invoke(currentTexture, SliceType.Sagittal, i);
            }

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
                }
                yield return null;

                currentTexture.Apply();
                TextureReady.Invoke(currentTexture, SliceType.Frontal, i);
            }

            FreeTextures();
            _workIndicator.FinishedWork();
        }

        /// <summary>
        /// Returns the Segment Texture for the given parameters.
        /// </summary>
        /// <param name="type">SliceType of the texture</param>
        /// <param name="index">Index of the texture</param>
        /// <returns>Texture2D containing the segment texture</returns>
        public Texture2D GetSegmentTexture(SliceType type, int index)
        {
            return _sliceSegments.GetValue(type)?[index];
        }

        /// <summary>
        /// Returns the Segment with the given index.
        /// </summary>
        /// <param name="index">Index from 0 and to MaxSegmentCount-1</param>
        /// <returns></returns>
        public Segment GetSegment(int index)
        {
            return _segments[index];
        }

        /// <summary>
        /// Clears the Segment with the given index.
        /// </summary>
        /// <param name="segmentIndex">Index from 0 and to MaxSegmentCount-1</param>
        public void Clear(int segmentIndex)
        {
            _segments[segmentIndex].Clear();
            SegmentChanged.Invoke(GetSelector(segmentIndex));
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
            for (var shift = 0; shift < MaxSegmentCount; shift++)
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

        /// <summary>
        /// Toggles the bit of the segment with index i.
        /// </summary>
        /// <param name="selector">The source selector</param>
        /// <param name="index">Index of the bit.</param>
        /// <returns>Modified selector.</returns>
        public static uint ToggleIndex(uint selector, int index)
        {            
            var sel = GetSelector(index);

            if ((sel & selector) == 0)
            {
                return selector | sel;
            }

            return selector & (~sel);
        }

        /// <summary>
        /// Locks access to the textures, to avoid multiple Coroutines working on them at the same time.
        /// </summary>
        /// <returns></returns>
        private IEnumerator AccessTextures()
        {
            while (_textureLock)
            {
                yield return null;
            }

            _textureLock = true;
        }
        
        /// <summary>
        /// Frees access to the textures
        /// </summary>
        private void FreeTextures()
        {
            _textureLock = false;
        }

        /// <summary>
        /// Locks access to the given volume, to avoid multiple Coroutines working on it at the same time.
        /// </summary>
        /// <param name="texture3D">Texture to lock access to</param>
        /// <returns></returns>
        private IEnumerator AccessVolume(Texture3D texture3D)
        {
            while (_volumeLocks.Contains(texture3D))
            {
                yield return null;
            }

            _volumeLocks.Add(texture3D);
        }

        /// <summary>
        /// Frees access to the given Volume
        /// </summary>
        /// <param name="texture3D">Texture to free</param>
        private void FreeVolume(Texture3D texture3D)
        {
            _volumeLocks.Remove(texture3D);
        }

    }
}