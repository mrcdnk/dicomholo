using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DICOMParser;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;

namespace DICOMData
{

    public class ThreadState
    {
        private static object wMutex = new object();
        private static object pMutex = new object();
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

        private NativeArray<int> data;

        private ThreadState state = new ThreadState
        {
            working = 0,
            progress = 0
        };

        private Texture2D[] transversalTexture2Ds;
        private Texture2D[] frontalTexture2Ds;
        private Texture2D[] sagittalTexture2Ds;

        private Texture3D volume;

        private Dictionary<int, DiFile> dicomFiles;

        private string folderPath;

        private int width;
        private int height;

        private double windowCenter = -1;
        private double windowWidth = -1;

        private bool expl = true;

        private bool initialize = false;

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

            progresshandler.update(state.progress);
        }

        private void OnDestroy()
        {
            data.Dispose();
        }

        public void Init()
        {
            initialize = true;
        }

        private IEnumerator init()
        {

            dicomFiles = new Dictionary<int, DiFile>();
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

            yield return null;
            expl = new DiDataElement(fileNames[0]).quickscanExp();

            progresshandler.init(fileNames.Count, "Loading Files");
            yield return null;
            foreach (var path in fileNames)
            {
                DiFile diFile = new DiFile(expl);
                diFile.initFromFile(path);
                dicomFiles.Add(diFile.getImageNumber(), diFile);
                progresshandler.increment(1);
                yield return null;
            }

            width = dicomFiles[0].getImageWidth();
            height = dicomFiles[0].getImageHeight();

            data = new NativeArray<int>(dicomFiles.Count * width * height, Allocator.Persistent);

            progresshandler.init(dicomFiles.Count, "Preprocessing Data");
            yield return null;

            startPreProcessing(state, dicomFiles, data);

            while (state.working > 0)
            {
                yield return null;
            }

            state.reset();

            progresshandler.init(dicomFiles.Count, "Creating Volume");

            volume = new Texture3D(width, height, dicomFiles.Count, TextureFormat.ARGB32, true);

            var cols = new Color[width * height * dicomFiles.Count];
            int idx = 0;
            int idxPartZ = 0;
            int idxPart = 0;
            for (int z = 0; z < dicomFiles.Count; ++z)
            {
                idxPartZ = z * width * height;
                for (int y = 0; y < height; ++y)
                {
                    idxPart = idxPartZ + y;
                    for (int x = 0; x < width; ++x, ++idx)
                    {
                        cols[idx] = PixelShader.DYN_ALPHA(GetRGBValue(data[idxPart + x * height], dicomFiles[z]));
                    }
                }
                state.progress++;
                yield return null;
            }
            //yield return null;

            volume.SetPixels(cols);
            volume.Apply();
            rayMarching.initVolume(volume);
            //renderTarget.GetComponent<Renderer>().material = Resources.Load<Material>("Volume");
            //renderTarget.GetComponent<Renderer>().material.SetTexture("_Volume", volume);

            /*transversalTexture2Ds = new Texture2D[dicomFiles.Count];
            frontalTexture2Ds = new Texture2D[height];
            sagittalTexture2Ds = new Texture2D[width];

            yield return null;

            Texture2D currentTex;
            Color32[] pixelColor32s;

            for (int layer = 0; layer < dicomFiles.Count; layer++)
            {
                currentTex = new Texture2D(width, height, TextureFormat.ARGB32, true);
                pixelColor32s = currentTex.GetPixels32();
                fillPixelsTransversal(layer, pixelColor32s);
                currentTex.SetPixels32(pixelColor32s);
                currentTex.Apply();
                transversalTexture2Ds[layer] = currentTex;
                progresshandler.increment(1);
                if (layer == 50)
                {
                    previewImage.texture = currentTex;
                }
                yield return null;
            }

            for (int y = 0; y < height; y++)
            {
                currentTex = new Texture2D(width, dicomFiles.Count, TextureFormat.ARGB32, true);
                pixelColor32s = currentTex.GetPixels32();
                fillPixelsFrontal(y, pixelColor32s);
                currentTex.SetPixels32(pixelColor32s);
                currentTex.Apply();
                frontalTexture2Ds[y] = currentTex;
                progresshandler.increment(1);
                yield return null;
            }

            for (int x = 0; x < width; x++)
            {
                currentTex = new Texture2D(height, dicomFiles.Count, TextureFormat.ARGB32, true);
                pixelColor32s = currentTex.GetPixels32();
                fillPixelsSagittal(x, pixelColor32s);
                currentTex.SetPixels32(pixelColor32s);
                currentTex.Apply();
                sagittalTexture2Ds[x] = currentTex;
                progresshandler.increment(1);
                yield return null;
            }

            viewmanager.ready(this);
            */

        }

        private static bool HasNoExtension(string f)
        {
            return !Regex.Match(f, @"[.]*\.[.]*").Success;
        }

        public NativeArray<int> GetData()
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
                case SliceType.TRANSVERSAL: return dicomFiles.Count;
                case SliceType.FRONTAL: return height;
                case SliceType.SAGITTAL: return width;
                default: return 0;
            }
        }

        public void fillPixelsTransversal(int id, Color32[] texData)
        {
            fillPixelsTransversal(id, texData, integer=>integer);
        }

        public void fillPixelsTransversal(int id, Color32[] texData, Func<Color32, Color32> pShader)
        {
            int idxPartId = id * width * height;
            int idxPart;
            DiFile file = dicomFiles[id];

            for (int y = 0; y < height; ++y)
            {
                idxPart = idxPartId + y;
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;

                    texData[index] = pShader(GetRGBValue(this.data[idxPart + x * height], file));
                }
            }
        }

        public void fillPixelsFrontal(int id, Color32[] texData)
        {
            fillPixelsFrontal(id, texData, integer=>integer);
        }

        public void fillPixelsFrontal(int id, Color32[] texData, Func<Color32, Color32> pShader)
        {

            int idxPart = 0;

            for (int i = 0; i < dicomFiles.Count; ++i)
            {
                idxPart = i * width * height + id;
                DiFile file = dicomFiles[i];
              
                for (int x = 0; x < width; ++x)
                {
                    int index = i * height + x;

                    texData[index] = pShader(GetRGBValue(this.data[idxPart + x * height], file));
                }
            }
        }

        public void fillPixelsSagittal(int id, Color32[] texData)
        {
            fillPixelsSagittal(id, texData, integer=>integer);
        }

        public void fillPixelsSagittal(int id, Color32[] texData, Func<Color32, Color32> pShader)
        {
            int idxPart = 0;

            for (int i = 0; i < dicomFiles.Count; ++i)
            {
                idxPart = i * width * height + id * height;
                DiFile file = dicomFiles[i];

                for (int y = 0; y < height; ++y)
                {
                    int index = i * width + y;

                    texData[index] = pShader(GetRGBValue(this.data[idxPart + y], file));
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

            return (int)intensity;
        }

        private Color32 GetRGBValue(int pixelIntensity, DiFile file)
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


            if ((windowCenterElement != null && windowWidthElement != null) || (windowCenter != -1 && windowWidth != -1))
            {
                double windowC =  windowCenter != -1 ? (double) windowCenter : windowCenterElement.getValueAsDouble();
                double windowW =  windowWidth != -1 ? (double) windowWidth : windowWidthElement.getValueAsDouble();

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

           

            return new Color32((byte)Math.Round(intensity), (byte)Math.Round(intensity), (byte)Math.Round(intensity), 255);
        }

        private void startPreProcessing(ThreadState state, Dictionary<int, DiFile> files, NativeArray<int> target)
        {
            state.working = 1;

            var t = new Thread(() => PreProcess(state, files, target, height, width, 0, files.Count));
            t.Start();
        }

        private static void PreProcess(ThreadState state, Dictionary<int, DiFile> files, NativeArray<int> target, int height, int width, int start, int end)
        {

            DiFile currenDiFile;
            byte[] storedBytes = new byte[4];

            for (int layer = start; layer < end; layer++)
            {
                currenDiFile = files[layer];
                DiDataElement pixelData = currenDiFile.getElement(0x7FE0, 0x0010);
                DiDataElement highBitElement = currenDiFile.getElement(0x0028, 0x0102);
                int mask = ~((~0) << highBitElement.getValueAsInt() + 1);
                int allocated = currenDiFile.getBitsAllocated() / 8;

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

                            target[layer * width * height + x * height + y] = currentPix;
                        }
                    }
                }

                state.progress++;
            }
            state.done();
        }
    }

    public enum SliceType
    {
        TRANSVERSAL, SAGITTAL, FRONTAL 
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
