using System;
using System.Collections.Generic;
using System.IO;
using DICOMParser;
using GLTF.Schema;
using HoloToolkit.UX.Progress;
using Segmentation;
using Threads;
using UnityEngine;

namespace DICOMViews
{
    /// <summary>
    /// Manages dependencies between all views and relays events to interested views.
    /// </summary>
    public class ViewManager : MonoBehaviour
    {
        public MainMenu MainMenu;
        public WindowSettingsPanel WindowSettingsPanel;
        public Slice2DView Slice2DView;

        public GameObject VolumeRenderingParent;
        public VolumeRendering.VolumeRendering VolumeRendering;

        public SegmentConfiguration SegmentConfiguration;

        public GameObject Volume;
        public RayMarching RayMarching;
        private ImageStack _stack;
        private SegmentCache _segmentCache;

        private readonly List<Tuple<ThreadGroupState, string, Action>> _currentWorkloads = new List<Tuple<ThreadGroupState, string, Action>>(5);


        // Use this for initialization
        void Start ()
        {
            MainMenu.ClearDropdown();

            var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

            var names = new List<string>();

            foreach (var fold in folders)
            {
                names.Add(fold.Split('\\')[1]);

            }

            MainMenu.AddDropdownOptions(names);

            _stack = gameObject.AddComponent<ImageStack>();
            _stack.ViewManager = this;

            _segmentCache = gameObject.AddComponent<SegmentCache>();
            _segmentCache.TextureReady.AddListener(SegmentTextureUpdated);
            _segmentCache.SegmentChanged.AddListener(SegmentChanged);

            Slice2DView.ImageStack = _stack;
            Slice2DView.SegmentCache = _segmentCache;

            WindowSettingsPanel.SettingsChangedEvent.AddListener(OnWindowSettingsChanged);
            WindowSettingsPanel.gameObject.SetActive(false);
            Volume.SetActive(false);
            VolumeRenderingParent.SetActive(false);
            Slice2DView.gameObject.SetActive(false);
            SegmentConfiguration.transform.gameObject.SetActive(false);

            Slice2DView.OnPointSelected.AddListener(SegmentConfiguration.UpdateRegionSeed);

            ParseFiles();
        }
	
        // Update is called once per frame
        void Update ()
        {
            int progress = 0;
            int index = 0;

            while (_currentWorkloads.Count > 0 && index < _currentWorkloads.Count)
            {
                var tuple = _currentWorkloads[index];
                if (tuple.Item1.Progress == tuple.Item1.TotalProgress && tuple.Item1.Working == 0)
                {
                    //Remove from list
                    RemoveWorkload(index);
                    if (_currentWorkloads.Count > 0)
                    {
                        MainMenu.ProgressHandler.TaskDescription = _currentWorkloads[0].Item2;
                        continue;
                    }
                    break;
                }

                progress += tuple.Item1.Progress;
                index++;
            }

            MainMenu.ProgressHandler.Value = progress;
        }

        private void OnWindowSettingsChanged(double winWidth, double winCenter)
        {
            _stack.WindowWidth = winWidth;
            _stack.WindowCenter = winCenter;
        }

        public void ParseFiles()
        {
            WindowSettingsPanel.DisableButtons();
            MainMenu.DisableButtons();
            WindowSettingsPanel.gameObject.SetActive(false);
            SegmentConfiguration.transform.gameObject.SetActive(false);
            AddWorkload(_stack.StartParsingFiles(Path.Combine(Application.streamingAssetsPath, MainMenu.GetSelectedFolder())),"Loading Files", OnFilesParsed);
        }

        private void OnFilesParsed()
        {
            WindowSettingsPanel.Configure(_stack.MinPixelIntensity, _stack.MaxPixelIntensity, _stack.WindowWidth, _stack.WindowCenter);
            WindowSettingsPanel.gameObject.SetActive(true);
            _segmentCache.InitializeSize(_stack.Width, _stack.Height, _stack.Slices);
            SegmentConfiguration.Initialize(_segmentCache, _stack.MinPixelIntensity, _stack.MaxPixelIntensity);
            Slice2DView.Initialize();
            PreProcessData();
        }

        public void PreProcessData()
        {
            AddWorkload(_stack.StartPreprocessData(), "Preprocessing Data", OnPreProcessDone);
        }

        private void OnPreProcessDone()
        {
            WindowSettingsPanel.EnableButtons();
            MainMenu.EnableButtons();
            _segmentCache.InitializeTextures();
            SegmentConfiguration.transform.gameObject.SetActive(true);
        }

        public void CreateVolume()
        {
            MainMenu.DisableButtons();
            AddWorkload(_stack.StartCreatingVolume(), "Creating Volume", OnVolumeCreated);
        }

        private void OnVolumeCreated()
        {
            VolumeRendering.SetVolume(_stack.VolumeTexture);
            StartCoroutine(_segmentCache.ApplySegments(_stack.VolumeTexture));
            //RayMarching.initVolume(_stack.VolumeTexture);

            //Volume.SetActive(true);
            MainMenu.EnableButtons();
            VolumeRenderingParent.SetActive(true);
        }

        public void CreateTextures()
        {
            MainMenu.DisableButtons();
            Slice2DView.gameObject.SetActive(true);
            AddWorkload(_stack.StartCreatingTextures(), "Creating Textures", OnTexturesCreated);
        }

        private void OnTexturesCreated()
        {
            MainMenu.EnableButtons();
        }

        public void TextureUpdated(SliceType type, int index)
        {
            Slice2DView.TextureUpdated(type, index);

            if (type == SliceType.Transversal && index == 50)
            {
                MainMenu.SetPreviewImage(_stack.GetTexture2D(type, index));
            }
        }

        private void SegmentChanged(uint selector)
        {
            //combine selector with user selection and apply it to the cache.
            StartCoroutine(_segmentCache.ApplyTextures(clearFlag: true));
            if (_stack.VolumeTexture)
            {
                StartCoroutine(_segmentCache.ApplySegments(_stack.VolumeTexture));
            }
        }

        private void SegmentTextureUpdated(Texture2D tex, SliceType type, int index)
        {
            Slice2DView.SegmentTextureUpdated(tex, type, index);
        }

        public void AddWorkload(ThreadGroupState threadGroupState, string description, Action onFinished)
        {
            _currentWorkloads.Add(new Tuple<ThreadGroupState, string, Action>(threadGroupState, description, onFinished));

            if (_currentWorkloads.Count == 1)
            {
                MainMenu.ProgressHandler.TaskDescription = description;
                MainMenu.ProgressHandler.Value = 0;
            }
            MainMenu.ProgressHandler.Max += threadGroupState.TotalProgress;
        }

        private void RemoveWorkload(int index)
        {
            var tuple = _currentWorkloads[index];
            tuple.Item3.Invoke();
            if (index == 0)
            {
                MainMenu.ProgressHandler.Value -= tuple.Item1.TotalProgress;
                MainMenu.ProgressHandler.Max -= tuple.Item1.TotalProgress;   
            }
            _currentWorkloads.RemoveAt(index);
        }

    }
}
