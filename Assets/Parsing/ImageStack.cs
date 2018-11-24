using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DICOMParser;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using System.Threading.Tasks;

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
        public Dropdown selection;
        public Progresshandler progresshandler;
        public RawImage previewImage;
        public Viewmanager viewmanager;
        public Button LoadButton;

        public RayMarching rayMarching;

        public GameObject renderTarget;

        public Text debug;

        private int[] data;

        private ThreadState threadState = new ThreadState
        {
            working = 0,
            progress = 0
        };

        private ThreadState previewState = new ThreadState
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

        private double windowCenter = -1;
        private double windowWidth = -1;

        private bool expl = true;

        private bool initialize = false;
        private bool useThreadState = false;

        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (initialize)
            {
                LoadButton.interactable = false;
                StartCoroutine("init");
                initialize = false;
            }

            if (useThreadState)
            {
                progresshandler.update(threadState.progress);
            }
        }

        public void Init()
        {
            initialize = true;
        }

        private IEnumerator waitForThreads()
        {
            while (threadState.working > 0)
            {
                yield return null;
            }
        }

        private IEnumerator init()
        {
            //var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

            /*foreach (var fold in folders)
            {
                if (fold.Contains(selection.captionText.text))
                {
                    folderPath = fold;
                }
                break;
            }*/

            //string[] filePaths = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, selection.captionText.text));

            //filePaths = Array.FindAll(filePaths, HasNoExtension);

            int pos = 0; //startfile

            List<string> fileNames = new List<string>();

            while (UnityEngine.Windows.File.Exists(Path.Combine(
                Path.Combine(Application.streamingAssetsPath, selection.captionText.text),
                "CTHd" + pos.ToString("D3"))))
            {
                fileNames.Add(Path.Combine(Path.Combine(Application.streamingAssetsPath, selection.captionText.text),
                    "CTHd" + pos.ToString("D3")));
                pos++;
            }

            dicomFiles = new DiFile[fileNames.Count];

            yield return null;
            expl = new DiDataElement(fileNames[0]).quickscanExp();

            progresshandler.init(fileNames.Count, "Loading Files");
            yield return null;
            foreach (var path in fileNames)
            {
                DiFile diFile = new DiFile(expl);
                diFile.initFromFile(path);
                dicomFiles[diFile.getImageNumber()] = diFile;
                progresshandler.increment(1);
                yield return null;
            }

            width = dicomFiles[0].getImageWidth();
            height = dicomFiles[0].getImageHeight();

            data = new int[dicomFiles.Length * width * height];

            progresshandler.init(dicomFiles.Length, "Preprocessing Data");
            yield return null;

            useThreadState = true;
            startPreProcessing(threadState, dicomFiles, data, 28);

            yield return waitForThreads();

            threadState.reset();

            progresshandler.init(dicomFiles.Length, "Creating Volume");

            volume = new Texture3D(width, height, dicomFiles.Length, TextureFormat.ARGB32, true);

            var cols = new Color[width * height * dicomFiles.Length];
            
            startCreatingVolume(threadState, dicomFiles, data, cols, 28);

            yield return waitForThreads();

            volume.SetPixels(cols);
            volume.Apply();
            rayMarching.initVolume(volume);

            threadState.reset();

            transversalTexture2Ds = new Texture2D[dicomFiles.Length];
            frontalTexture2Ds = new Texture2D[height];
            sagittalTexture2Ds = new Texture2D[width];

            Color32[][] transTextureColors = new Color32[dicomFiles.Length][];
            Color32[][] frontTextureColors = new Color32[height][];
            Color32[][] sagTextureColors = new Color32[width][];

            progresshandler.init(dicomFiles.Length + height + width, "Creating Textures");

            ConcurrentQueue<int> transProgress = new ConcurrentQueue<int>();
            ConcurrentQueue<int> frontProgress = new ConcurrentQueue<int>();
            ConcurrentQueue<int> sagProgress = new ConcurrentQueue<int>();

            startCreatingTransTextures(threadState, transProgress, data, dicomFiles, transTextureColors, 1);
            startCreatingFrontTextures(threadState, frontProgress, data, dicomFiles, frontTextureColors, 1);
            startCreatingSagTextures(threadState, sagProgress, data, dicomFiles, sagTextureColors, 1);

            while (threadState.working > 0)
            {
                int current;

                while (transProgress.TryDequeue(out current))
                {
                    transversalTexture2Ds[current] = new Texture2D(width, height, TextureFormat.ARGB32, true);
                    transversalTexture2Ds[current].SetPixels32(transTextureColors[current]);
                    transversalTexture2Ds[current].Apply();
                    if (current == 50)
                    {
                        previewImage.texture = transversalTexture2Ds[current];
                    }

                    yield return null;
                }

                while (frontProgress.TryDequeue(out current))
                {
                    frontalTexture2Ds[current] = new Texture2D(width, dicomFiles.Length, TextureFormat.ARGB32, true);
                    frontalTexture2Ds[current].SetPixels32(frontTextureColors[current]);
                    frontalTexture2Ds[current].Apply();
                    yield return null;
                }

                while (sagProgress.TryDequeue(out current))
                {
                    sagittalTexture2Ds[current] = new Texture2D(height, dicomFiles.Length, TextureFormat.ARGB32, true);
                    sagittalTexture2Ds[current].SetPixels32(sagTextureColors[current]);
                    sagittalTexture2Ds[current].Apply();
                    yield return null;
                }

                yield return null;
            }

            viewmanager.ready(this);
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

        public static void fillPixelsTransversal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData)
        {
            fillPixelsTransversal(id, data, width, height, files, texData, integer => integer);
        }

        public static void fillPixelsTransversal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData, Func<Color32, Color32> pShader)
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

                    texData[index] = pShader(GetRGBValue(data[idxPart + x * height], file));
                }
            }
        }

        public static void fillPixelsFrontal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData)
        {
            fillPixelsFrontal(id, data, width, height, files, texData, integer => integer);
        }

        public static void fillPixelsFrontal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData, Func<Color32, Color32> pShader)
        {
            int idxPart;

            for (int i = 0; i < files.Length; ++i)
            {
                idxPart = i * width * height + id;
                DiFile file = files[i];

                for (int x = 0; x < width; ++x)
                {
                    int index = i * height + x;

                    texData[index] = pShader(GetRGBValue(data[idxPart + x * height], file));
                }
            }
        }

        public static void fillPixelsSagittal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData)
        {
            fillPixelsSagittal(id, data, width, height, files, texData, integer => integer);
        }

        public static void fillPixelsSagittal(int id, int[] data, int width, int height, DiFile[] files, Color32[] texData, Func<Color32, Color32> pShader)
        {
            int idxPart;

            for (int i = 0; i < files.Length; ++i)
            {
                idxPart = i * width * height + id * height;
                DiFile file = files[i];

                for (int y = 0; y < height; ++y)
                {
                    int index = i * width + y;

                    texData[index] = pShader(GetRGBValue(data[idxPart + y], file));
                }
            }
        }

        private static int getPixelIntensity(int pixelIntensity, DiFile file)
        {
            DiDataElement interceptElement = file.getElement(0x0028, 0x1052);
            DiDataElement slopeElement = file.getElement(0x0028, 0x1053);

            double intercept = 0;
            double slope = 1;

            if (interceptElement != null)
            {
                intercept = interceptElement.getValueAsDouble();
            }

            if (slopeElement != null)
            {
                slope = slopeElement.getValueAsDouble();
            }

            double intensity = (pixelIntensity * slope) + intercept;

            return (int) intensity;
        }

        private static Color32 GetRGBValue(int pixelIntensity, DiFile file, float windowWidth = -1,
            float windowCenter = -1)
        {
            int bitsStored = file.getBitsStored();

            DiDataElement windowCenterElement = file.getElement(0x0028, 0x1050);
            DiDataElement windowWidthElement = file.getElement(0x0028, 0x1051);
            DiDataElement interceptElement = file.getElement(0x0028, 0x1052);
            DiDataElement slopeElement = file.getElement(0x0028, 0x1053);
            double intercept = 0;
            double slope = 1;

            if (interceptElement != null)
            {
                intercept = interceptElement.getValueAsDouble();
            }

            if (slopeElement != null)
            {
                slope = slopeElement.getValueAsDouble();
            }

            double intensity = pixelIntensity;


            if ((windowCenterElement != null && windowWidthElement != null) ||
                (windowCenter != -1 && windowWidth != -1))
            {
                double windowC = windowCenter != -1 ? (double) windowCenter : windowCenterElement.getValueAsDouble();
                double windowW = windowWidth != -1 ? (double) windowWidth : windowWidthElement.getValueAsDouble();

                if (intensity < windowC - (windowW / 2))
                {
                    intensity = 0;
                }
                else if (intensity > windowC + (windowW / 2))
                {
                    intensity = 255;
                }
                else
                {
                    //0 for rgb min value and 255 for rgb max value
                    intensity = (((intensity - (windowC - 0.5f)) / (windowW - 1)) + 0.5f) * 255f;
                }
            }
            else
            {
                double oldMax = Math.Pow(2, bitsStored) * slope + intercept;
                double oRange = oldMax - intercept;
                double rgbRange = 255;

                intensity = (((intensity - intercept) * rgbRange) / oRange) + 0;
            }

            return new Color32((byte) Math.Round(intensity), (byte) Math.Round(intensity), (byte) Math.Round(intensity),
                255);
        }

        private void startPreProcessing(ThreadState state, DiFile[] files, int[] target, int threadCount)
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

                var t = new Thread(() => preProcess(state, files, width, height, target, startIndex, endIndex));
                t.Start();
            }
        }

        private void preProcess(ThreadState state, DiFile[] files, int width, int height,
            int[] target,
            int start, int end)
        {
            DiFile currenDiFile;
            byte[] storedBytes = new byte[4];

            for (int layer = start; layer < end; ++layer)
            {
                currenDiFile = files[layer];
                DiDataElement pixelData = currenDiFile.removeElement(0x7FE0, 0x0010);
                DiDataElement highBitElement = currenDiFile.getElement(0x0028, 0x0102);
                int mask = ~((~0) << highBitElement.getValueAsInt() + 1);
                int allocated = currenDiFile.getBitsAllocated() / 8;

                int indlwh = layer * width * height;

                using (MemoryStream pixels = new MemoryStream(pixelData.GetValues()))
                {
                    int currentPix;

                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            //get current Int value
                            pixels.Read(storedBytes, 0, allocated);

                            int value = BitConverter.ToInt32(storedBytes, 0);

                            currentPix = getPixelIntensity(value & mask, currenDiFile);

                            target[indlwh + x * height + y] = currentPix;
                        }
                    }
                }

                state.incrementProgress();
            }

            state.done();
        }

        private void startCreatingVolume(ThreadState state, DiFile[] files, int[] data, Color[] target, int threadCount)
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

                var t = new Thread(() => createVolume(state, data, files, width, height, target, startIndex, endIndex));
                t.Start();
            }
        }

        private static void createVolume(ThreadState state, int[] data, DiFile[] dicomFiles, int width, int height,
            Color[] target, int start, int end)
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
                        target[idx] = PixelShader.DYN_ALPHA(GetRGBValue(data[idxPart + x * height], dicomFiles[z]));
                    }
                }

                state.incrementProgress();
            }

            state.done();
        }

        private void startCreatingTransTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, DiFile[] files, Color32[][] target, int threadCount)
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

                var t = new Thread(() => createTransTextures(state, processed, data, width, height, files, target, startIndex, endIndex));
                t.Start();
            }
        }

        private static void createTransTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, int width, int height, DiFile[] files, Color32[][] target, int start, int end)
        {
            for (int layer = start; layer < end; ++layer)
            {
                target[layer] = new Color32[width*height];
                fillPixelsTransversal(layer, data, width, height, files, target[layer]);
                processed.Enqueue(layer);
                state.incrementProgress();
            }

            state.done();
        }

        private void startCreatingFrontTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, DiFile[] files, Color32[][] target, int threadCount)
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

                var t = new Thread(() => createFrontTextures(state, processed, data, width, height, files, target, startIndex, endIndex));
                t.Start();
            }
        }

        private static void createFrontTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, int width, int height, DiFile[] files, Color32[][] target, int start, int end)
        {
            for (int y = start; y < end; ++y)
            {
                target[y] = new Color32[width * files.Length];
                fillPixelsFrontal(y, data, width, height, files, target[y]);
                processed.Enqueue(y);
                state.incrementProgress();
            }

            state.done();
        }

        private void startCreatingSagTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, DiFile[] files, Color32[][] target, int threadCount)
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

                var t = new Thread(() => createSagTextures(state, processed, data, width, height, files, target, startIndex, endIndex));
                t.Start();
            }
        }

        private static void createSagTextures(ThreadState state, ConcurrentQueue<int> processed, int[] data, int width, int height, DiFile[] files, Color32[][] target, int start, int end)
        {
            for (int x = start; x < end; ++x)
            {
                target[x] = new Color32[height * files.Length];
                fillPixelsSagittal(x, data, width, height, files, target[x]);
                processed.Enqueue(x);
                state.incrementProgress();
            }

            state.done();
        }
    }

    public enum SliceType
    {
        TRANSVERSAL,
        SAGITTAL,
        FRONTAL
    }

    public class PixelShader
    {
        private PixelShader()
        {
        }

        public static Color32 DYN_ALPHA(Color32 argb)
        {
            double dynAlpha = 210 * (Math.Max(argb.r - 10, 0)) / 255d;
            argb.a = (byte) dynAlpha;
            return argb;
        }
    }
}