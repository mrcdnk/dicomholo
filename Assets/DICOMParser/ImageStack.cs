using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using DICOMViews;
using Threads;
using System.Threading;
using UnityEngine.Events;

namespace DICOMParser
{

    /// <summary>
    /// Contains all DICOM data and data generated from it.
    /// </summary>
    public class ImageStack : MonoBehaviour
    {

        private int[] _data;

        private Texture2D[] _transversalTexture2Ds;
        private Texture2D[] _frontalTexture2Ds;
        private Texture2D[] _sagittalTexture2Ds;
        
        private DiFile[] _dicomFiles;

        public Text DebugText;
        public Texture3D VolumeTexture { get; private set; }

        private string _folderPath;

        public double WindowCenter { get; set; } = double.MinValue;
        public double WindowWidth { get; set; } = double.MinValue;

        public double[] WindowCenterPresets { get; private set; }
        public double[] WindowWidthPresets { get; private set; }

        public int MinPixelIntensity { get; set; }
        public int MaxPixelIntensity { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Slices => _dicomFiles?.Length ?? 0;

        public TextureUpdate OnTextureUpdate = new TextureUpdate();

        /// <summary>
        /// Start coroutine for parsing of files.
        /// </summary>
        public ThreadGroupState StartParsingFiles(string folderPath)
        {
            ThreadGroupState state = new ThreadGroupState();
            StartCoroutine(InitFiles(folderPath, state));
            return state;
        }

        /// <summary>
        /// Starts coroutine for preprocessing DICOM pixeldata
        /// </summary>
        public ThreadGroupState StartPreprocessData()
        {
            ThreadGroupState state = new ThreadGroupState {TotalProgress = _dicomFiles.Length};
            PreprocessData(state);
            return state;
        }

        /// <summary>
        /// Starts coroutine for creating the 3D texture
        /// </summary>
        public ThreadGroupState StartCreatingVolume()
        {
            ThreadGroupState state = new ThreadGroupState {TotalProgress = _dicomFiles.Length };
            StartCoroutine(CreateVolume(state));

            return state;
        }

        /// <summary>
        /// Start coroutine for creating 2D textures.
        /// </summary>
        public ThreadGroupState StartCreatingTextures()
        {
            ThreadGroupState state = new ThreadGroupState {TotalProgress = _dicomFiles.Length + Width + Height};
            StartCoroutine(CreateTextures(state));
            return state;
        }


        /// <summary>
        /// Allows a Unity coroutine to wait for every working thread to finish.
        /// </summary>
        /// <param name="threadGroupState">Thread safe thread-state used to observe progress of one or multiple threads.</param>
        /// <returns>IEnumerator for usage as a coroutine</returns>
        private static IEnumerator WaitForThreads(ThreadGroupState threadGroupState)
        {
            while (threadGroupState.Working > 0)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Unity coroutine for loading the selected folder of files.
        /// </summary>
        /// <param name="folderPath">Path of the folder containing the DICOM files</param>
        /// <param name="threadGroupState">Thread safe thread-state used to observe progress of one or multiple threads.</param>
        /// <returns>IEnumerator for usage as a coroutine</returns>
        private IEnumerator InitFiles(string folderPath, ThreadGroupState threadGroupState)
        {
            threadGroupState.Register();
            //string[] filePaths = Directory.GetFiles(folderPath);

            //filePaths = Array.FindAll(filePaths, HasNoExtension); 
            List<string> fileNames = new List<string>();

#if UNITY_EDITOR
            foreach (var file in Directory.GetFiles(folderPath))
            {
                if (UnityEngine.Windows.File.Exists(file) && (file.EndsWith(".dcm") || !file.Contains(".")))
                {
                    fileNames.Add(file);
                }
            }

#elif UNITY_WSA
            var pos = 0; //startfile

            while (UnityEngine.Windows.File.Exists(folderPath+ "/CTHd" + pos.ToString("D3")))
            {
                fileNames.Add(Path.Combine(folderPath, "CTHd" + pos.ToString("D3")));
                ++pos;
            }
#endif
            _dicomFiles = new DiFile[fileNames.Count];
            threadGroupState.TotalProgress = fileNames.Count;

            yield return null;

            var zeroBased = true;

            foreach (var path in fileNames)
            {
                DiFile diFile = new DiFile();
                diFile.InitFromFile(path);

                if (zeroBased && diFile.GetImageNumber() == _dicomFiles.Length)
                {
                    ShiftLeft(_dicomFiles);
                    zeroBased = false;
                } 

                if (zeroBased)
                {
                    _dicomFiles[diFile.GetImageNumber()] = diFile;
                }
                else
                {
                    _dicomFiles[diFile.GetImageNumber()-1] = diFile;
                }

                threadGroupState.IncrementProgress();
                yield return null;
            }

            Width = _dicomFiles[0].GetImageWidth();
            Height = _dicomFiles[0].GetImageHeight();

            _data = new int[_dicomFiles.Length * Width * Height];

            VolumeTexture = null;

            WindowCenterPresets = _dicomFiles[0].GetElement(0x0028, 0x1050)?.GetDoubles() ?? new[]{double.MinValue};
            WindowWidthPresets = _dicomFiles[0].GetElement(0x0028, 0x1051)?.GetDoubles() ?? new[]{double.MinValue};

            WindowCenter = WindowCenterPresets[0];
            WindowWidth = WindowWidthPresets[0];

            MinPixelIntensity = (int)(_dicomFiles[0].GetElement(0x0028, 0x1052)?.GetDouble() ?? 0d); 
            MaxPixelIntensity = (int)((_dicomFiles[0].GetElement(0x0028, 0x1053)?.GetDouble() ?? 1d)*(Math.Pow(2, _dicomFiles[0].GetBitsStored())-1)+MinPixelIntensity);

            threadGroupState.Done();
        }

        /// <summary>
        /// Shifts every element in the given list to the left.
        /// </summary>
        /// <typeparam name="T">Type of the list</typeparam>
        /// <param name="array">List to be modified</param>
        private static void ShiftLeft<T>(IList<T> array)
        {
            for (var i = 1; i < array.Count; i++)
            {
                array[i - 1] = array[i];
            }
        }

        /// <summary>
        /// Unity coroutine used to preprocess the DICOM pixel data using multiple threads.
        /// </summary>
        /// <param name="threadGroupState"></param>
        /// <returns>IEnumerator for usage as a coroutine</returns>
        private void PreprocessData(ThreadGroupState threadGroupState)
        {
            StartPreProcessing(threadGroupState, _dicomFiles, _data, 12);
        }

        /// <summary>
        /// Unity coroutine used to create the 3D texture using multiple threads.
        /// </summary>
        /// <param name="threadGroupState">Thread safe thread-state used to observe progress of one or multiple threads.</param>
        /// <returns>IEnumerator for usage as a coroutine</returns>
        private IEnumerator CreateVolume(ThreadGroupState threadGroupState)
        {
            VolumeTexture = new Texture3D(Width, Height, _dicomFiles.Length, TextureFormat.ARGB32, false);

            var cols = new Color32[Width * Height * _dicomFiles.Length];

            StartCreatingVolume(threadGroupState, _dicomFiles, _data, cols, WindowWidth, WindowCenter, 6);

            yield return WaitForThreads(threadGroupState);

            VolumeTexture.SetPixels32(cols);
            VolumeTexture.Apply();
        }

        /// <summary>
        /// Unity coroutine used to create all textures using multiple threads.
        /// </summary>
        /// <param name="threadGroupState">Thread safe thread-state used to observe progress of one or multiple threads.</param>
        /// <returns>IEnumerator for usage as a coroutine</returns>
        private IEnumerator CreateTextures(ThreadGroupState threadGroupState)
        {
            _transversalTexture2Ds = new Texture2D[_dicomFiles.Length];
            _frontalTexture2Ds = new Texture2D[Height];
            _sagittalTexture2Ds = new Texture2D[Width];

            var transTextureColors = new Color32[_dicomFiles.Length][];
            var frontTextureColors = new Color32[Height][];
            var sagTextureColors = new Color32[Width][];

            var transProgress = new ConcurrentQueue<int>();
            var frontProgress = new ConcurrentQueue<int>();
            var sagProgress = new ConcurrentQueue<int>();

            StartCreatingTransTextures(threadGroupState, transProgress, _data, _dicomFiles, transTextureColors, WindowWidth, WindowCenter, 2);
            StartCreatingFrontTextures(threadGroupState, frontProgress, _data, _dicomFiles, frontTextureColors, WindowWidth, WindowCenter, 2);
            StartCreatingSagTextures(threadGroupState, sagProgress, _data, _dicomFiles, sagTextureColors, WindowWidth, WindowCenter, 2);

            while (threadGroupState.Working > 0 || !(transProgress.IsEmpty && frontProgress.IsEmpty && sagProgress.IsEmpty))
            {
                int current;
                if (transProgress.TryDequeue(out current))
                {
                    CreateTexture2D(Width, Height, transTextureColors, _transversalTexture2Ds, current);
                    OnTextureUpdate.Invoke(SliceType.Transversal, current);
                    threadGroupState.IncrementProgress();
                }

                if (frontProgress.TryDequeue(out current))
                {
                    CreateTexture2D(Width, _dicomFiles.Length, frontTextureColors, _frontalTexture2Ds, current);
                    OnTextureUpdate.Invoke(SliceType.Frontal, current);
                    threadGroupState.IncrementProgress();
                }

                if (sagProgress.TryDequeue(out current))
                {
                    CreateTexture2D(Height, _dicomFiles.Length, sagTextureColors, _sagittalTexture2Ds, current);
                    OnTextureUpdate.Invoke(SliceType.Sagittal, current);
                    threadGroupState.IncrementProgress();
                }

                yield return null;
            }

        }

        /// <summary>
        /// Creates a new Texture 2D with given width, height and Colors and writes it to the target Texture2D array. Also notifies the viewmanager of the texture update.
        /// </summary>
        /// <param name="width">Width of the to be created texture.</param>
        /// <param name="height">Height of the to be created texture.</param>
        /// <param name="textureColors">Array of all colors for every slice of this slice type.</param>
        /// <param name="target">Target array to write the texture2D to.</param>
        /// <param name="current">Index of the Texture to be created.</param>
        private static void CreateTexture2D(int width, int height, IReadOnlyList<Color32[]> textureColors, IList<Texture2D> target, int current)
        {
            var currentTexture2D = new Texture2D(width, height, TextureFormat.ARGB32, true);
            currentTexture2D.SetPixels32(textureColors[current]);
            currentTexture2D.filterMode = FilterMode.Point;
            currentTexture2D.Apply();
            Destroy(target[current]);
            target[current] = currentTexture2D;
        }

        /// <summary>
        /// Checks if a file has an extension.
        /// </summary>
        /// <param name="f">the filename</param>
        /// <returns>true if the file has no extension.</returns>
        private static bool HasNoExtension(string f)
        {
            return !Regex.Match(f, @"[.]*\.[.]*").Success;
        }

        /// <summary>
        /// Returns the raw data array containing the intensity values.
        /// </summary>
        /// <returns>The raw 3D array with intensity values.</returns>
        public int[] GetData()
        {
            return _data;
        }

        /// <summary>
        /// Returns the Texture2D with the given index of the given SliceType.
        /// </summary>
        /// <param name="type">Requested SliceType</param>
        /// <param name="index">The index of the texture</param>
        /// <returns></returns>
        public Texture2D GetTexture2D(SliceType type, int index)
        {
            switch (type)
            {
                case SliceType.Transversal: return _transversalTexture2Ds?[index];
                case SliceType.Frontal: return _frontalTexture2Ds?[index];
                case SliceType.Sagittal: return _sagittalTexture2Ds?[index];
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Checks if the arrays containing the slices exist.
        /// </summary>
        /// <param name="type">Requested SliceType</param>
        /// <returns>True if the corresponding array is not null</returns>
        public bool HasData(SliceType type)
        {
            switch (type)
            {
                case SliceType.Transversal:
                    return _transversalTexture2Ds != null;
                case SliceType.Frontal:
                    return _frontalTexture2Ds != null;
                case SliceType.Sagittal:
                    return _sagittalTexture2Ds != null;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the maximum value for the given slice type, starting from 0.
        /// </summary>
        /// <param name="type">Requested SliceType</param>
        /// <returns>Max Value for the SliceType</returns>
        public int GetMaxValue(SliceType type)
        {
            switch (type)
            {
                case SliceType.Transversal: return _dicomFiles.Length - 1;
                case SliceType.Frontal: return Height-1;
                case SliceType.Sagittal: return Width-1;
                default: return 0;
            }
        }

        /// <summary>
        /// Starts one or more Threads for preprocessing.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">1D array receiving the 3D data.</param>
        /// <param name="threadCount">Amount of Threads to use.</param>
        private void StartPreProcessing(ThreadGroupState groupState, IReadOnlyList<DiFile> files, int[] target, int threadCount)
        {
            int spacing = files.Count / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                var startIndex = i * spacing;
                var endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = files.Count;
                }

                groupState.Register();
                var t = new Thread(() => PreProcess(groupState, files, Width, Height, target, startIndex, endIndex))
                {
                    IsBackground = true
                };
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target array with 3D data while applying basic preprocessing.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="width">width of a DICOM slice.</param>
        /// <param name="height">height of a DICOM slice.</param>
        /// <param name="target">1D array receiving the 3D data.</param>
        /// <param name="start">Start index used to determine partition of images to be computed</param>
        /// <param name="end">End index used to determine upper bound of partition of images to be computed</param>
        private static void PreProcess(ThreadGroupState groupState, IReadOnlyList<DiFile> files, int width, int height,
            IList<int> target, int start, int end)
        {
            var storedBytes = new byte[4];           

            for (var layer = start; layer < end; ++layer)
            {
                var currentDiFile = files[layer];
                var pixelData = currentDiFile.RemoveElement(0x7FE0, 0x0010);
                uint mask = ~(0xFFFFFFFF << (currentDiFile.GetHighBit()+1));
                int allocated = currentDiFile.GetBitsAllocated() / 8;

                var baseOffset = layer * width * height;

                using (var pixels = new MemoryStream(pixelData.GetValues()))
                {
                    for (var y = 0; y < height; ++y)
                    {
                        for (var x = 0; x < width; ++x)
                        {
                            //get current Int value
                            pixels.Read(storedBytes, 0, allocated);
                            var value = BitConverter.ToInt32(storedBytes, 0);
                            var currentPix = GetPixelIntensity((int)(value & mask), currentDiFile);
                            target[baseOffset + x * height + y] = currentPix;
                        }
                    }

                }

                groupState.IncrementProgress();
                Thread.Sleep(10);
            }
         
            groupState.Done();
        }

        /// <summary>
        /// Starts threads for volume texture creation.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingVolume(ThreadGroupState groupState, IReadOnlyList<DiFile> files, IReadOnlyList<int> data, IList<Color32> target, double windowWidth, double windowCenter, int threadCount)
        {
            var spacing = files.Count / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                groupState.Register();
                var startIndex = i * spacing;
                var endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = files.Count;
                }

                var t = new Thread(() => CreateVolume(groupState, data, files, Width, Height, target, windowWidth,
                    windowCenter, startIndex, endIndex)) {IsBackground = true};
                t.Start();
            }
        }

        /// <summary>
        /// Fills the given 3D color array using the given 3D pixel intensity array of same size.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="dicomFiles">all the DICOM files.</param>
        /// <param name="width">width of a transversal image.</param>
        /// <param name="height">height of a transversal image.</param>
        /// <param name="target">§D color array mapped to 1D Array.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="start">Start index used to determine partition of images to be computed</param>
        /// <param name="end">End index used to determine upper bound of partition of images to be computed</param>
        private static void CreateVolume(ThreadGroupState groupState, IReadOnlyList<int> data, IReadOnlyList<DiFile> dicomFiles, int width, int height,
            IList<Color32> target, double windowWidth, double windowCenter, int start, int end)
        {
            var idx = start*width*height;

            for (var z = start; z < end; ++z)
            {
                var idxPartZ = z * width * height;
                for (var y = 0; y < height; ++y)
                {
                    var idxPart = idxPartZ + y;
                    for (var x = 0; x < width; ++x, ++idx)
                    {
                        
                        target[idx] = TransferFunction.DYN_ALPHA(GetRGBValue(data[idxPart + x * height], dicomFiles[z], windowWidth, windowCenter));
                    }
                }

                Thread.Sleep(5);
                groupState.IncrementProgress();
            }

            groupState.Done();
        }

        /// <summary>
        /// Starts threads for transversal texture creation.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingTransTextures(ThreadGroupState groupState, ConcurrentQueue<int> processed, int[] data, IReadOnlyList<DiFile> files, IList<Color32[]> target,
            double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = files.Count / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                groupState.Register();
                var startIndex = i * spacing;
                var endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = files.Count;
                }

                var t = new Thread(() => CreateTransTextures(groupState, processed, data, Width, Height, files,
                    target,
                    windowWidth, windowCenter, startIndex, endIndex)) {IsBackground = true};
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target color array with the pixels for all transversal images in range from start to end (excluding end).
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="width">width of a transversal image.</param>
        /// <param name="height">height of a transversal image.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="start">Start index used to determine partition of images to be computed</param>
        /// <param name="end">End index used to determine upper bound of partition of images to be computed</param>
        private static void CreateTransTextures(ThreadGroupState groupState, ConcurrentQueue<int> processed, int[] data, int width, int height, IReadOnlyList<DiFile> files, 
            IList<Color32[]> target, double windowWidth, double windowCenter, int start, int end)
        {
            for (var layer = start; layer < end; ++layer)
            {
                target[layer] = new Color32[width*height];
                FillPixelsTransversal(layer, data, width, height, files, target[layer], TransferFunction.Identity, windowWidth, windowCenter);
                processed.Enqueue(layer);
                Thread.Sleep(5);
            }

            groupState.Done();
        }

        /// <summary>
        /// Starts threads for frontal texture creation.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingFrontTextures(ThreadGroupState groupState, ConcurrentQueue<int> processed, int[] data, IReadOnlyList<DiFile> files, Color32[][] target,
            double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = Height / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                groupState.Register();
                var startIndex = i * spacing;
                var endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = Height;
                }

                var t = new Thread(() => CreateFrontTextures(groupState, processed, data, Width, Height, files,
                    target,
                    windowWidth, windowCenter, startIndex, endIndex)) {IsBackground = true};
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target color array with the pixels for all frontal images in range from start to end (excluding end).
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="width">width of a transversal image.</param>
        /// <param name="height">height of a transversal image.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="start">Start index used to determine partition of images to be computed</param>
        /// <param name="end">End index used to determine upper bound of partition of images to be computed</param>
        private static void CreateFrontTextures(ThreadGroupState groupState, ConcurrentQueue<int> processed, int[] data, int width, int height, IReadOnlyList<DiFile> files, IList<Color32[]> target,
            double windowWidth, double windowCenter, int start, int end)
        {
            for (var y = start; y < end; ++y)
            {
                target[y] = new Color32[width * files.Count];
                FillPixelsFrontal(y, data, width, height, files, target[y], TransferFunction.Identity, windowWidth, windowCenter);
                processed.Enqueue(y);
                Thread.Sleep(5);
            }

            groupState.Done();
        }

        /// <summary>
        /// Starts threads for sagittal texture creation.
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingSagTextures(ThreadGroupState groupState, ConcurrentQueue<int> processed, int[] data, IReadOnlyList<DiFile> files, IList<Color32[]> target,
            double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = Width / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                groupState.Register();
                var startIndex = i * spacing;
                var endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = Width;
                }

                var t = new Thread(() => CreateSagTextures(groupState, processed, data, Width, Height, files, target,
                    windowWidth, windowCenter, startIndex, endIndex)) {IsBackground = true};
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target color array with the pixels for all saggital images in range from start to end (excluding end).
        /// </summary>
        /// <param name="groupState">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="width">width of a transversal image.</param>
        /// <param name="height">height of a transversal image.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="start">Start index used to determine partition of images to be computed</param>
        /// <param name="end">End index used to determine upper bound of partition of images to be computed</param>
        private static void CreateSagTextures(ThreadGroupState groupState, ConcurrentQueue<int> processed, int[] data, int width, int height, IReadOnlyList<DiFile> files, IList<Color32[]> target,
            double windowWidth, double windowCenter, int start, int end)
        {
            for (var x = start; x < end; ++x)
            {
                target[x] = new Color32[height * files.Count];
                FillPixelsSagittal(x, data, width, height, files, target[x], TransferFunction.Identity, windowWidth, windowCenter);
                processed.Enqueue(x);
                Thread.Sleep(5);
            }

            groupState.Done();
        }

        /// <summary>
        /// Wrapper for simple calls without specific shader or windowwidth/Center
        /// </summary>
        /// <param name="id">Image number</param>
        /// <param name="data">3D Pixel intensity data</param>
        /// <param name="width">transversal slice width</param>
        /// <param name="height">transversal slice height</param>
        /// <param name="files">array of all DICOM files</param>
        /// <param name="texData">target texture array</param>
        public static void FillPixelsTransversal(int id, int[] data, int width, int height, IReadOnlyList<DiFile> files, Color32[] texData)
        {
            FillPixelsTransversal(id, data, width, height, files, texData, TransferFunction.Identity);
        }

        /// <summary>
        /// Fills a Color32 Array with transversal texture data from the pixelIntensity values given by the data array.
        /// </summary>
        /// <param name="id">Image number</param>
        /// <param name="data">3D Pixel intensity data</param>
        /// <param name="width">transversal slice width</param>
        /// <param name="height">transversal slice height</param>
        /// <param name="files">array of all DICOM files</param>
        /// <param name="texData">target texture array</param>
        /// <param name="pShader">pixel shader to be applied to every pixel</param>
        /// <param name="windowWidth">Optional possibility to override windowWidth</param>
        /// <param name="windowCenter">Optional possibility to override windowCenter</param>
        public static void FillPixelsTransversal(int id, int[] data, int width, int height, IReadOnlyList<DiFile> files, Color32[] texData,
            Func<Color32, Color32> pShader, double windowWidth = double.MinValue, double windowCenter = double.MinValue)
        {
            var idxPartId = id * width * height;
            var file = files[id];

            for (var y = 0; y < height; ++y)
            {
                var idxPart = idxPartId + y;
                for (var x = 0; x < width; ++x)
                {
                    var index = y * width + x;

                    texData[index] = pShader(GetRGBValue(data[idxPart + x * height], file, windowWidth, windowCenter));
                }
            }
        }

        /// <summary>
        /// Wrapper for simple calls without specific shader or windowwidth/Center
        /// </summary>
        /// <param name="id">Image number</param>
        /// <param name="data">3D Pixel intensity data</param>
        /// <param name="width">transversal slice width</param>
        /// <param name="height">transversal slice height</param>
        /// <param name="files">array of all DICOM files</param>
        /// <param name="texData">target texture array</param>
        public static void FillPixelsFrontal(int id, int[] data, int width, int height, IReadOnlyList<DiFile> files, Color32[] texData)
        {
            FillPixelsFrontal(id, data, width, height, files, texData, TransferFunction.Identity);
        }

        /// <summary>
        /// Fills a Color32 Array with frontal texture data from the pixelIntensity values given by the data array.
        /// </summary>
        /// <param name="id">Image number</param>
        /// <param name="data">3D Pixel intensity data</param>
        /// <param name="width">transversal slice width</param>
        /// <param name="height">transversal slice height</param>
        /// <param name="files">array of all DICOM files</param>
        /// <param name="texData">target texture array</param>
        /// <param name="pShader">pixel shader to be applied to every pixel</param>
        /// <param name="windowWidth">Optional possibility to override windowWidth</param>
        /// <param name="windowCenter">Optional possibility to override windowCenter</param>
        public static void FillPixelsFrontal(int id, int[] data, int width, int height, IReadOnlyList<DiFile> files, Color32[] texData,
            Func<Color32, Color32> pShader, double windowWidth = double.MinValue, double windowCenter = double.MinValue)
        {
            for (var i = 0; i < files.Count; ++i)
            {
                var idxPart = i * width * height + id;
                var file = files[i];

                for (var x = 0; x < width; ++x)
                {
                    var index = i * height + x;

                    texData[index] = pShader(GetRGBValue(data[idxPart + x * height], file, windowWidth, windowCenter));
                }
            }
        }

        /// <summary>
        /// Wrapper for simple calls without specific shader or windowwidth/Center
        /// </summary>
        /// <param name="id">Image number</param>
        /// <param name="data">3D Pixel intensity data</param>
        /// <param name="width">transversal slice width</param>
        /// <param name="height">transversal slice height</param>
        /// <param name="files">array of all DICOM files</param>
        /// <param name="texData">target texture array</param>
        public static void FillPixelsSagittal(int id, int[] data, int width, int height, IReadOnlyList<DiFile> files, Color32[] texData)
        {
            FillPixelsSagittal(id, data, width, height, files, texData, TransferFunction.Identity);
        }

        /// <summary>
        /// Fills a Color32 Array with sagittal texture data from the pixelIntensity values given by the data array.
        /// </summary>
        /// <param name="id">Image number</param>
        /// <param name="data">3D Pixel intensity data</param>
        /// <param name="width">transversal slice width</param>
        /// <param name="height">transversal slice height</param>
        /// <param name="files">array of all DICOM files</param>
        /// <param name="texData">target texture array</param>
        /// <param name="pShader">pixel shader to be applied to every pixel</param>
        /// <param name="windówWidth">Optional possibility to override windowWidth</param>
        /// <param name="windowCenter">Optional possibility to override windowCenter</param>
        public static void FillPixelsSagittal(int id, int[] data, int width, int height, IReadOnlyList<DiFile> files, Color32[] texData,
            Func<Color32, Color32> pShader, double windówWidth = double.MinValue, double windowCenter = double.MinValue)
        {
            for (var i = 0; i < files.Count; ++i)
            {
                var idxPart = i * width * height + id * height;
                var file = files[i];

                for (var y = 0; y < height; ++y)
                {
                    var index = i * width + y;

                    texData[index] = pShader(GetRGBValue(data[idxPart + y], file, windówWidth, windowCenter));
                }
            }
        }

        /// <summary>
        /// Applies intercept and slope of the given DiFile.
        /// </summary>
        /// <param name="rawIntensity">Raw pixel intensity</param>
        /// <param name="file">DiFile containing the pixel</param>
        /// <returns>The resulting value</returns>
        private static int GetPixelIntensity(int rawIntensity, DiFile file)
        {
            var interceptElement = file.GetElement(0x0028, 0x1052);
            var slopeElement = file.GetElement(0x0028, 0x1053);

            var intercept = interceptElement?.GetDouble() ?? 0;
            var slope = slopeElement?.GetDouble() ?? 1;
            var intensity = (rawIntensity * slope) + intercept;

            return (int)intensity;
        }

        /// <summary>
        /// Computes the RGB Value for an intensity value.
        /// </summary>
        /// <param name="pixelIntensity">Intensity value of a pixel</param>
        /// <param name="file">DICOM File containing the pixel</param>
        /// <param name="windowWidth">Option to set own window width</param>
        /// <param name="windowCenter">Option to set own window center</param>
        /// <returns>The resulting Color</returns>
        private static Color32 GetRGBValue(int pixelIntensity, DiFile file, double windowWidth = double.MinValue,
            double windowCenter = double.MinValue)
        {
            var bitsStored = file.GetBitsStored();
            const int rgbRange = 255;

            var windowCenterElement = file.GetElement(0x0028, 0x1050);
            var windowWidthElement = file.GetElement(0x0028, 0x1051);
            var interceptElement = file.GetElement(0x0028, 0x1052);
            var slopeElement = file.GetElement(0x0028, 0x1053);

            var intercept = interceptElement?.GetDouble() ?? 0;
            var slope = slopeElement?.GetDouble() ?? 1;
            double intensity = pixelIntensity;

            if (windowCenter > double.MinValue && windowWidth > double.MinValue)
            {
                intensity = ApplyWindow(intensity, windowWidth, windowCenter);
            }
            else if (windowCenterElement != null && windowWidthElement != null)
            {
                intensity = ApplyWindow(pixelIntensity, windowWidthElement.GetDouble(),
                    windowCenterElement.GetDouble());
            }
            else
            {
                var oldMax = Math.Pow(2, bitsStored) * slope + intercept;
                var oRange = oldMax - intercept;

                intensity = ((intensity - intercept) * rgbRange) / oRange;
            }
            var result = (byte)Math.Round(intensity);

            return new Color32(result, result, result, rgbRange);
        }

        /// <summary>
        /// Applies the windowWidth and windowCenter attributes from a DICOM file.
        /// </summary>
        /// <param name="val">intensity value the window will be applied to</param>
        /// <param name="width">width of the window</param>
        /// <param name="center">center of the window</param>
        /// <returns></returns>
        public static double ApplyWindow(double val, double width, double center)
        {
            var intensity = val;

            if (intensity <= center - 0.5 - (width-1) / 2)
            {
                intensity = 0;
            }
            else if (intensity > center - 0.5 + (width-1) / 2)
            {
                intensity = 255;
            }
            else
            {
                //0 for rgb min value and 255 for rgb max value
                intensity = ((intensity - (center - 0.5)) / (width - 1) + 0.5) * 255;
            }

            return intensity;
        }

        public class TextureUpdate : UnityEvent<SliceType, int>{}

    }
}