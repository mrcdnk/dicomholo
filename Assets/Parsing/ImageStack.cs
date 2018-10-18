using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DICOMParser;
using System;
using System.IO;
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
        }

        private static bool HasNoExtension(string f)
        {
            return !Regex.Match(f, @"[.]*\.[.]*").Success;
        }

        public uint getRGBValue(int pixelIntensity, DiFile file)
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
}
