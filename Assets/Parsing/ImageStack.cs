using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DICOMParser;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using DICOMViews;

namespace DICOMData
{
    public class ThreadState
    {
        private object wMutex = new object();
        private object pMutex = new object();
        public int progress;
        public int working;

        public void reset()
        {
            progress = 0;
            working = 0;
        }

        public void incrementProgress()
        {
            lock (pMutex)
            {
                progress++;
            }
        }

        public void register()
        {
            lock (wMutex)
            {
                working++;
            }
        }

        public void done()
        {
            lock (wMutex)
            {
                working--;
            }
        }
    }

    public class ImageStack : MonoBehaviour
    {
        public Dropdown Selection;
        public ProgressHandler ProgressHandler;
        public RawImage PreviewImage;
        public ViewManager ViewManager;

        public Button LoadVolumeButton;
        public Button Load2DButton;

        public Slice2DView Slice2DView;
        public GameObject Volume;

        public VolumeRendering.VolumeRendering VolumeRendering;

        public RayMarching RayMarching;

        public Text debug;

        private int[] data;

        private ThreadState threadState = new ThreadState
        {
            working = 0,
            progress = 0
        };

        private Texture2D[] transversalTexture2Ds;
        private Texture2D[] frontalTexture2Ds;
        private Texture2D[] sagittalTexture2Ds;

        private Texture3D volume;

        private DiFile[] dicomFiles;

        private string folderPath;

        private int width;
        private int height;

        private double windowCenter = Double.MinValue;
        private double windowWidth = Double.MinValue;

        private bool expl = true;

        private bool useThreadState = false;

        // Use this for initialization
        void Start()
        {
            LoadVolumeButton.interactable = false;
            Load2DButton.interactable = false;
            Volume.SetActive(false);
            Slice2DView.SetVisible(false);

            //Load first selected entry in dropdown
            StartInitFiles();
        }

        // Update is called once per frame
        void Update()
        {
            if (useThreadState)
            {
                ProgressHandler.update(threadState.progress);
            }
        }

        public void StartInitFiles()
        {
            LoadVolumeButton.interactable = false;
            Load2DButton.interactable = false;
            StartCoroutine(nameof(InitFiles));
        }

        private void StartPreprocessData()
        {
            StartCoroutine(nameof(PreprocessData));
        }

        public void StartCreatingVolume()
        {
            LoadVolumeButton.interactable = false;
            Load2DButton.interactable = false;
            StartCoroutine(nameof(CreateVolume));
        }

        public void StartCreatingTextures()
        {
            Slice2DView.SetVisible(true);
            LoadVolumeButton.interactable = false;
            Load2DButton.interactable = false;
            StartCoroutine(nameof(CreateTextures));
        }

        private IEnumerator WaitForThreads()
        {
            while (threadState.working > 0)
            {
                yield return null;
            }
        }

        private IEnumerator InitFiles()
        {
            //var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

            /*foreach (var fold in folders)
            {
                if (fold.Contains(Selection.captionText.text))
                {
                    folderPath = fold;
                }
                break;
            }*/

            //string[] filePaths = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, Selection.captionText.text));

            //filePaths = Array.FindAll(filePaths, HasNoExtension); 
            List<string> fileNames = new List<string>();

            /*foreach (string file in Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, Selection.captionText.text)))
            {
                if (UnityEngine.Windows.File.Exists(file) && (file.EndsWith(".dcm") || !file.Contains(".")))
                {
                    fileNames.Add(file);
                }
            }*/

            int pos = 0; //startfile

            while (UnityEngine.Windows.File.Exists(Path.Combine(
                Path.Combine(Application.streamingAssetsPath, Selection.captionText.text),
                "CTHd" + pos.ToString("D3"))))
            {
                fileNames.Add(Path.Combine(Path.Combine(Application.streamingAssetsPath, Selection.captionText.text),
                    "CTHd" + pos.ToString("D3")));
                pos++;
            }

            dicomFiles = new DiFile[fileNames.Count];

            yield return null;
            expl = new DiDataElement(fileNames[0]).quickscanExp();

            ProgressHandler.init(fileNames.Count, "Loading Files");
            yield return null;

            foreach (var path in fileNames)
            {
                DiFile diFile = new DiFile(expl);
                diFile.InitFromFile(path);
                dicomFiles[diFile.GetImageNumber()] = diFile;
                ProgressHandler.increment(1);
                yield return null;
            }

            width = dicomFiles[0].GetImageWidth();
            height = dicomFiles[0].GetImageHeight();

            data = new int[dicomFiles.Length * width * height];

            StartPreprocessData();
        }

        private IEnumerator PreprocessData()
        {
            threadState.reset();

            ProgressHandler.init(dicomFiles.Length, "Preprocessing Data");
            yield return null;

            useThreadState = true;
            StartPreProcessing(threadState, dicomFiles, data, 20);

            yield return WaitForThreads();

            LoadVolumeButton.interactable = true;
            Load2DButton.interactable = true;
        }

        private IEnumerator CreateVolume()
        {
            threadState.reset();

            ProgressHandler.init(dicomFiles.Length, "Creating Volume");

            volume = new Texture3D(width, height, dicomFiles.Length, TextureFormat.ARGB32, true);

            var cols = new Color[width * height * dicomFiles.Length];

            StartCreatingVolume(threadState, dicomFiles, data, cols, windowWidth, windowCenter, 10);

            yield return WaitForThreads();

            Load2DButton.interactable = true;

            volume.SetPixels(cols);
            volume.Apply();

            VolumeRendering.SetVolume(volume);
            RayMarching.initVolume(volume);

            Volume.SetActive(true);
        }

        private IEnumerator CreateTextures()
        {    
            threadState.reset();

            transversalTexture2Ds = new Texture2D[dicomFiles.Length];
            frontalTexture2Ds = new Texture2D[height];
            sagittalTexture2Ds = new Texture2D[width];

            Color32[][] transTextureColors = new Color32[dicomFiles.Length][];
            Color32[][] frontTextureColors = new Color32[height][];
            Color32[][] sagTextureColors = new Color32[width][];

            ProgressHandler.init(dicomFiles.Length + height + width, "Creating Textures");

            ConcurrentQueue<int> transProgress = new ConcurrentQueue<int>();
            ConcurrentQueue<int> frontProgress = new ConcurrentQueue<int>();
            ConcurrentQueue<int> sagProgress = new ConcurrentQueue<int>();

            StartCreatingTransTextures(threadState, transProgress, data, dicomFiles, transTextureColors, windowCenter, windowWidth, 2);
            StartCreatingFrontTextures(threadState, frontProgress, data, dicomFiles, frontTextureColors, windowCenter, windowWidth, 2);
            StartCreatingSagTextures(threadState, sagProgress, data, dicomFiles, sagTextureColors, windowCenter, windowWidth, 2);
            
            while (threadState.working > 0 || !(transProgress.IsEmpty && frontProgress.IsEmpty && sagProgress.IsEmpty))
            {
                int current;
                Texture2D currentTexture2D;

                while (transProgress.TryDequeue(out current))
                {
                    currentTexture2D = new Texture2D(width, height, TextureFormat.ARGB32, true);
                    currentTexture2D.SetPixels32(transTextureColors[current]);
                    currentTexture2D.filterMode = FilterMode.Point;
                    currentTexture2D.Apply();
                    transversalTexture2Ds[current] = currentTexture2D;

                    if (current == 50)
                    {
                        PreviewImage.texture = transversalTexture2Ds[current];
                    }

                    if (Slice2DView != null)
                    {
                        Slice2DView.TextureUpdated(SliceType.TRANSVERSAL, current);
                    }

                    yield return null;
                }

                while (frontProgress.TryDequeue(out current))
                {
                    currentTexture2D = new Texture2D(width, dicomFiles.Length, TextureFormat.ARGB32, true);
                    currentTexture2D.SetPixels32(frontTextureColors[current]);
                    currentTexture2D.filterMode = FilterMode.Point;
                    currentTexture2D.Apply();
                    frontalTexture2Ds[current] = currentTexture2D;

                    if (Slice2DView != null)
                    {
                        Slice2DView.TextureUpdated(SliceType.FRONTAL, current);
                    }

                    yield return null;
                }

                while (sagProgress.TryDequeue(out current))
                {
                    currentTexture2D = new Texture2D(height, dicomFiles.Length, TextureFormat.ARGB32, true);
                    currentTexture2D.SetPixels32(sagTextureColors[current]);
                    currentTexture2D.filterMode = FilterMode.Point;
                    currentTexture2D.Apply();
                    sagittalTexture2Ds[current] = currentTexture2D;

                    if (Slice2DView != null)
                    {
                        Slice2DView.TextureUpdated(SliceType.SAGITTAL, current);
                    }

                    yield return null;
                }

                yield return null;
            }

            LoadVolumeButton.interactable = true;
        }

        private static bool HasNoExtension(string f)
        {
            return !Regex.Match(f, @"[.]*\.[.]*").Success;
        }

        public int[] GetData()
        {
            return data;
        }

        public Texture2D GetTexture2D(SliceType type, int id)
        {
            switch (type)
            {
                case SliceType.TRANSVERSAL: return transversalTexture2Ds[id];
                case SliceType.FRONTAL: return frontalTexture2Ds[id];
                case SliceType.SAGITTAL: return sagittalTexture2Ds[id];
            }

            return null;
        }

        public bool HasData(SliceType type)
        {
            switch (type)
            {
                case SliceType.TRANSVERSAL: return transversalTexture2Ds != null;
                case SliceType.FRONTAL: return frontalTexture2Ds != null;
                case SliceType.SAGITTAL: return sagittalTexture2Ds != null;
            }

            return false;
        }

        public int GetMaxValue(SliceType type)
        {
            switch (type)
            {
                case SliceType.TRANSVERSAL: return dicomFiles.Length;
                case SliceType.FRONTAL: return height;
                case SliceType.SAGITTAL: return width;
                default: return 0;
            }
        }

        private void StartPreProcessing(ThreadState state, DiFile[] files, int[] target, int threadCount)
        {
            windowCenter = Double.MinValue;
            windowWidth = Double.MinValue;

            int spacing = files.Length / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                state.register();
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = files.Length;
                }

                var t = new Thread(() => PreProcess(state, files, width, height, target, startIndex, endIndex));
                t.Start();
            }
        }

        private static void PreProcess(ThreadState state, DiFile[] files, int width, int height,
            int[] target, int start, int end)
        {
            DiFile currentDiFile = null;
            var storedBytes = new byte[4];
            int debug = start;
           

            for (int layer = start; layer < end; ++layer)
            {
                debug = layer;
                currentDiFile = files[layer];
                DiDataElement pixelData = currentDiFile.RemoveElement(0x7FE0, 0x0010);
                int mask = ~(~0 << currentDiFile.GetHighBit() + 1);
                int allocated = currentDiFile.GetBitsAllocated() / 8;

                int indlwh = layer * width * height;

                using (var pixels = new MemoryStream(pixelData.GetValues()))
                {
                    int currentPix;

                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            //get current Int value
                            pixels.Read(storedBytes, 0, allocated);

                            var value = BitConverter.ToInt32(storedBytes, 0);

                            currentPix = GetPixelIntensity(value & mask, currentDiFile);

                            target[indlwh + x * height + y] = currentPix;
                        }
                    }

                }

                state.incrementProgress();
            }
         
            state.done();
        }

        /// <summary>
        /// Starts threads for volume texture creation.
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingVolume(ThreadState state, DiFile[] files, int[] data, Color[] target, double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = files.Length / threadCount;

            for (int i = 0; i < threadCount; ++i)
            {
                state.register();
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = files.Length;
                }

                var t = new Thread(() => createVolume(state, data, files, width, height, target, windowWidth, windowCenter, startIndex, endIndex));
                t.Priority = System.Threading.ThreadPriority.Lowest;
                t.Start();
            }
        }

        /// <summary>
        /// Fills the given 3D color array using the given 3D pixel intensity array of same size.
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="dicomFiles">all the DICOM files.</param>
        /// <param name="width">width of a transversal image.</param>
        /// <param name="height">height of a transversal image.</param>
        /// <param name="target">§D color array mapped to 1D Array.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="start">Start index used to determine partition of images to be computed</param>
        /// <param name="end">End index used to determine upper bound of partition of images to be computed</param>
        private static void createVolume(ThreadState state, int[] data, DiFile[] dicomFiles, int width, int height,
            Color[] target, double windowWidth, double windowCenter, int start, int end)
        {
            int idx = start*width*height;
            int idxPartZ;
            int idxPart;
            for (int z = start; z < end; ++z)
            {
                idxPartZ = z * width * height;
                for (int y = 0; y < height; ++y)
                {
                    idxPart = idxPartZ + y;
                    for (int x = 0; x < width; ++x, ++idx)
                    {
                        target[idx] = PixelShader.DYN_ALPHA(GetRGBValue(data[idxPart + x * height], dicomFiles[z], windowWidth, windowCenter));
                    }
                }

                state.incrementProgress();
            }

            state.done();
        }

        /// <summary>
        /// Starts threads for transversal texture creation.
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingTransTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, DiFile[] files, Color32[][] target,
            double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = files.Length / threadCount;

            for (int i = 0; i < threadCount; ++i)
            {
                state.register();
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = files.Length;
                }

                var t = new Thread(() => CreateTransTextures(state, processed, data, width, height, files, target,
                    windowWidth, windowCenter, startIndex, endIndex));
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target color array with the pixels for all transversal images in range from start to end (excluding end).
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
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
        private static void CreateTransTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, int width, int height, DiFile[] files, 
            Color32[][] target, double windowWidth, double windowCenter, int start, int end)
        {
            for (int layer = start; layer < end; ++layer)
            {
                target[layer] = new Color32[width*height];
                FillPixelsTransversal(layer, data, width, height, files, target[layer], PixelShader.Identity, windowWidth, windowCenter);
                processed.Enqueue(layer);
                state.incrementProgress();
            }

            state.done();
        }

        /// <summary>
        /// Starts threads for frontal texture creation.
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingFrontTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, DiFile[] files, Color32[][] target,
            double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = height / threadCount;

            for (int i = 0; i < threadCount; ++i)
            {
                state.register();
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = height;
                }

                var t = new Thread(() => CreateFrontTextures(state, processed, data, width, height, files, target,
                    windowWidth, windowCenter, startIndex, endIndex));
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target color array with the pixels for all frontal images in range from start to end (excluding end).
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
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
        private static void CreateFrontTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, int width, int height, DiFile[] files, Color32[][] target,
            double windowWidth, double windowCenter, int start, int end)
        {
            for (int y = start; y < end; ++y)
            {
                target[y] = new Color32[width * files.Length];
                FillPixelsFrontal(y, data, width, height, files, target[y], PixelShader.Identity, windowWidth, windowCenter);
                processed.Enqueue(y);
                state.incrementProgress();
            }

            state.done();
        }

        /// <summary>
        /// Starts threads for saggital texture creation.
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
        /// <param name="processed">synchronized queue which will be filled with each image index, that is ready.</param>
        /// <param name="data">pixel intensity values in a 3D Array mapped to a 1D Array.</param>
        /// <param name="files">all the DICOM files.</param>
        /// <param name="target">target jagged array, which the result will be written to.</param>
        /// <param name="windowWidth">Option to set custom windowWidth, Double.MinValue to not use it</param>
        /// <param name="windowCenter">Option to set custom windowCenter, Double.MinValue to not use it</param>
        /// <param name="threadCount">Amount of Threads to use</param>
        private void StartCreatingSagTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, DiFile[] files, Color32[][] target,
            double windowWidth, double windowCenter, int threadCount)
        {
            int spacing = width / threadCount;

            for (int i = 0; i < threadCount; ++i)
            {
                state.register();
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = width;
                }

                var t = new Thread(() => CreateSagTextures(state, processed, data, width, height, files, target,
                    windowWidth, windowCenter, startIndex, endIndex));
                t.Start();
            }
        }

        /// <summary>
        /// Fills the target color array with the pixels for all saggital images in range from start to end (excluding end).
        /// </summary>
        /// <param name="state">synchronized Threadstate used to observe progress of one or multiple threads.</param>
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
        private static void CreateSagTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, int width, int height, DiFile[] files, Color32[][] target,
            double windowWidth, double windowCenter, int start, int end)
        {
            for (int x = start; x < end; ++x)
            {
                target[x] = new Color32[height * files.Length];
                FillPixelsSagittal(x, data, width, height, files, target[x], PixelShader.Identity, windowWidth, windowCenter);
                processed.Enqueue(x);
                state.incrementProgress();
            }

            state.done();
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
        public static void FillPixelsTransversal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData)
        {
            FillPixelsTransversal(id, data, width, height, files, texData, PixelShader.Identity);
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
        /// <param name="windówWidth">Optional possibility to override windowWidth</param>
        /// <param name="windowCenter">Optional possibility to override windowCenter</param>
        public static void FillPixelsTransversal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData,
            Func<Color32, Color32> pShader, double windówWidth = Double.MinValue, double windowCenter = Double.MinValue)
        {
            int idxPartId = id * width * height;
            int idxPart;
            DiFile file = files[id];

            for (int y = 0; y < height; ++y)
            {
                idxPart = idxPartId + y;
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;

                    texData[index] = pShader(GetRGBValue(data[idxPart + x * height], file, windówWidth, windowCenter));
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
        public static void FillPixelsFrontal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData)
        {
            FillPixelsFrontal(id, data, width, height, files, texData, PixelShader.Identity);
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
        /// <param name="windówWidth">Optional possibility to override windowWidth</param>
        /// <param name="windowCenter">Optional possibility to override windowCenter</param>
        public static void FillPixelsFrontal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData,
            Func<Color32, Color32> pShader, double windowWidth = Double.MinValue, double windowCenter = Double.MinValue)
        {
            int idxPart;

            for (int i = 0; i < files.Length; ++i)
            {
                idxPart = i * width * height + id;
                DiFile file = files[i];

                for (int x = 0; x < width; ++x)
                {
                    int index = i * height + x;

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
        public static void FillPixelsSagittal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData)
        {
            FillPixelsSagittal(id, data, width, height, files, texData, PixelShader.Identity);
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
        public static void FillPixelsSagittal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData,
            Func<Color32, Color32> pShader, double windówWidth = Double.MinValue, double windowCenter = Double.MinValue)
        {
            int idxPart;

            for (int i = 0; i < files.Length; ++i)
            {
                idxPart = i * width * height + id * height;
                DiFile file = files[i];

                for (int y = 0; y < height; ++y)
                {
                    int index = i * width + y;

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
            DiDataElement interceptElement = file.GetElement(0x0028, 0x1052);
            DiDataElement slopeElement = file.GetElement(0x0028, 0x1053);

            double intercept = interceptElement?.GetDouble() ?? 0;
            double slope = slopeElement?.GetDouble() ?? 1;
            double intensity = (rawIntensity * slope) + intercept;

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
        private static Color32 GetRGBValue(int pixelIntensity, DiFile file, double windowWidth = Double.MinValue,
          double windowCenter = Double.MinValue)
        {
            int bitsStored = file.GetBitsStored();

            DiDataElement windowCenterElement = file.GetElement(0x0028, 0x1050);
            DiDataElement windowWidthElement = file.GetElement(0x0028, 0x1051);
            DiDataElement interceptElement = file.GetElement(0x0028, 0x1052);
            DiDataElement slopeElement = file.GetElement(0x0028, 0x1053);

            double intercept = interceptElement?.GetDouble() ?? 0;
            double slope = slopeElement?.GetDouble() ?? 1;
            double intensity = pixelIntensity;

            if (windowCenter != Double.MinValue && windowWidth != Double.MinValue)
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
                double oldMax = Math.Pow(2, bitsStored) * slope + intercept;
                double oRange = oldMax - intercept;
                double rgbRange = 255;

                intensity = ((intensity - intercept) * rgbRange) / oRange;
            }

            var result = (byte)Math.Round(intensity);

            return new Color32(result, result, result, 255);
        }

        /// <summary>
        /// Applies the windowWidth and windowCenter attributes from a DICOM file.
        /// </summary>
        /// <param name="val">intensity value the window will be applied to</param>
        /// <param name="width">width of the window</param>
        /// <param name="center">center of the window</param>
        /// <returns></returns>
        private static double ApplyWindow(double val, double width, double center)
        {
            double intensity = val;

            if (intensity < center - (width / 2))
            {
                intensity = 0;
            }
            else if (intensity > center + (width / 2))
            {
                intensity = 255;
            }
            else
            {
                //0 for rgb min value and 255 for rgb max value
                intensity = ((intensity - (center - 0.5f)) / (width - 1) + 0.5f) * 255f;
            }

            return intensity;
        }

    }

    /// <summary>
    /// SliceType Describes from which perspective the slice is generated
    /// </summary>
    public enum SliceType
    {
        TRANSVERSAL, // The images as they are stored inside the DICOM File
        SAGITTAL, // Side view
        FRONTAL // View from the Front
    }

    /// <summary>
    /// This class contains Color32 => Color32 functions that can be applied when processing the pixels contained inside a DICOM file.
    /// </summary>
    public class PixelShader
    {
        private PixelShader(){}

        /// <summary>
        /// Dynamic alpha calculation based on grey rgb(x,x,x) value, where x is the intensity.
        /// </summary>
        /// <param name="argb">input color (assumed r=g=b)</param>
        /// <returns>input color with calculated alpha value</returns>
        public static Color32 DYN_ALPHA(Color32 argb)
        {
            double dynAlpha = 210 * (Math.Max(argb.r - 10, 0)) / 255d;
            argb.a = (byte) dynAlpha;
            return argb;
        }

        /// <summary>
        /// Identity function color32 -> color32 
        /// </summary>
        /// <param name="argb"> input color </param>
        /// <returns>unchanged input color</returns>
        public static Color32 Identity(Color32 argb)
        {
            return argb;
        }
    }
}