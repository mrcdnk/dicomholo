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

        private int[][][] data;

        private List<DiFile> dicomFiles;

        private string folderPath;

        private int width;
        private int height;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void init()
        {
            
            width = 0;
            height = 0;

            dicomFiles = new List<DiFile>();
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

            while (UnityEngine.Windows.File.Exists(Path.Combine(Path.Combine(Application.streamingAssetsPath, selection.captionText.text), "CTHd" +pos.ToString("D3"))))
            {
                fileNames.Add(Path.Combine(Path.Combine(Application.streamingAssetsPath, selection.captionText.text), "CTHd" + pos.ToString("D3")));
                pos++;
            }
            progresshandler.init(fileNames.Count, "Parsing Files");

            foreach (var path in fileNames)
            {
                DiFile diFile = new DiFile(true);
                diFile.initFromFile(path);
                dicomFiles.Add(diFile);
                progresshandler.increment(1);
                debug.text += "\n" + path + " : " + progresshandler.getProgress();
            }
        }

        private static bool HasNoExtension(string f)
        {
            return !Regex.Match(f, @"[.]*\.[.]*").Success;
        }
    }
}
