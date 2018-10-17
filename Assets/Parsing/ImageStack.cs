using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using DICOMParser;

namespace DICOMData
{

    public class ImageStack : MonoBehaviour
    {

        public Dropdown selection;
        public Progresshandler progresshandler;

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
            var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

            foreach (var fold in folders)
            {
                if (fold.Contains(selection.captionText.text))
                {
                    folderPath = fold;
                }
                break;
            }

            string[] filePaths = Directory.GetFiles(folderPath);

            progresshandler.init(filePaths.Length);
        }
    }
}
