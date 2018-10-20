using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DICOMParser;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine.Windows;

namespace DICOMData
{

    public class ImageStack : MonoBehaviour
    {

        public Dropdown selection;
        public Progresshandler progresshandler;

        public Text debug;

        private int[,,] data;

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
                StartCoroutine("init");
                initialize = false;
            }
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

            data = new int[dicomFiles.Count, width, height];

            progresshandler.init(dicomFiles.Count, "Preprocessing Data");
            yield return null;

            DiFile currenDiFile;
            byte[] storedBytes = new byte[4];

            for (int layer = 0; layer < dicomFiles.Count; layer++)
            {
                currenDiFile = dicomFiles[layer];
                DiDataElement pixelData = currenDiFile.removeElement(0x7FE0, 0x0010);
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
                            currentPix = getPixelIntensity(BitConverter.ToInt32(storedBytes, 0), currenDiFile);
                            data[layer, x, y] = currentPix & mask;
                        }
                    }
                }

                progresshandler.increment(1);
                yield return null;
            }
        }

        private static bool HasNoExtension(string f)
        {
            return !Regex.Match(f, @"[.]*\.[.]*").Success;
        }

        public int[,,] GetData()
        {
            return data;
        }

        public void fillPixelsTransversal(int id, uint[] texData)
        {
            fillPixelsTransversal(id, texData, integer=>integer);
        }

        public void fillPixelsTransversal(int id, uint[] texData, Func<uint, uint> pShader)
        {
            DiFile file = dicomFiles[id];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;

                    texData[index] = pShader(GetRGBValue(this.data[id, x, y], file));
                }
            }
        }

        public void fillPixelsFrontal(int id, uint[] data)
        {
            fillPixelsFrontal(id, data, integer=>integer);
        }

        public void fillPixelsFrontal(int id, uint[] data, Func<uint, uint> pShader)
        {
            for (int i = 0; i < dicomFiles.Count; ++i)
            {

                DiFile file = dicomFiles[i];
              
                for (int x = 0; x < width; ++x)
                {
                    int index = i * height + x;

                    data[index] = pShader(GetRGBValue(this.data[i, x, id], file));
                }
            }
        }

        public void fillPixelsSagittal(int id, uint[] data)
        {
            fillPixelsSagittal(id, data, integer=>integer);
        }

        public void fillPixelsSagittal(int id, uint[] data, Func<uint, uint> pShader)
        {
            for (int i = 0; i < dicomFiles.Count; ++i)
            {

                DiFile file = dicomFiles[i];

                for (int y = 0; y < height; ++y)
                {
                    int index = i * width + y;

                    data[index] = pShader(GetRGBValue(this.data[i, id, y], file));
                }
            }
        }

        private int getPixelIntensity(int pixelIntensity, DiFile file)
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

        private uint GetRGBValue(int pixelIntensity, DiFile file)
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


            if (windowCenterElement != null && windowWidthElement != null || windowCenter != -1 && windowWidth != -1)
            {
                double windowC = (Double) windowCenter != -1 ? (Double) windowCenter : windowCenterElement.getValueAsDouble();
                double windowW = (Double) windowWidth != -1 ? (Double) windowWidth : windowWidthElement.getValueAsDouble();

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
                    intensity = (((intensity - (windowC - 0.5f)) / (windowW - 1)) + 0.5f) * 255;
                }
            }
            else
            {
                double oldMax = Math.Pow(2, bitsStored) * slope + intercept;
                double oRange = (oldMax - intercept);
                double rgbRange = 255;

                intensity = (((intensity - intercept) * rgbRange) / oRange) + 0;
            }

            return 0xff000000 | (uint)Math.Round(intensity) << 16 | (uint)Math.Round(intensity) << 8 | (uint)Math.Round(intensity);
        }
    }

    public class PixelShader
    {
        private PixelShader()
        {

        }

        public static uint DYN_ALPHA(uint argb)
        {
            uint r = (argb & 0x00FF0000) >> 16;
            uint dynAlpha = (uint)(210 * ((Math.Max(r - 10, 0)) / 255d));
            return (0x00FFFFFF & argb) | (dynAlpha << 24);
        }
    }
}
