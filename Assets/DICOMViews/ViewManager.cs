using System;
using System.Collections.Generic;
using System.IO;
using DICOMParser;
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
        private ImageStack _stack;
        private SegmentCache _segmentCache;
        private GlobalWorkIndicator _workIndicator;

        private readonly List<Tuple<ThreadGroupState, string, Action>> _currentWorkloads = new List<Tuple<ThreadGroupState, string, Action>>(5);

        public MainMenu MainMenu;
        public WindowSettingsPanel WindowSettingsPanel;
        public Slice2DView Slice2DView;

        public GameObject VolumeRenderingParent;
        public VolumeRendering.VolumeRendering VolumeRendering;

        public SegmentConfiguration SegmentConfiguration;

        public GameObject Volume;
        public RayMarching RayMarching;

        // Use this for initialization
        private void Start ()
        {
            _workIndicator = FindObjectOfType<GlobalWorkIndicator>();
            MainMenu.ClearDropdown();

            var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

            var names = new List<string>();

            foreach (var fold in folders)
            {
                names.Add(fold.Split('\\')[1]);
            }

            MainMenu.AddDropdownOptions(names);

            _stack = gameObject.AddComponent<ImageStack>();
            _stack.OnTextureUpdate.AddListener(TextureUpdated);
            _stack.OnTextureUpdate.AddListener(Slice2DView.TextureUpdated);

            _segmentCache = gameObject.AddComponent<SegmentCache>();
            _segmentCache.TextureReady.AddListener(Slice2DView.SegmentTextureUpdated);
            _segmentCache.SegmentChanged.AddListener(SegmentChanged);

            Slice2DView.SegmentCache = _segmentCache;

            WindowSettingsPanel.SettingsChangedEvent.AddListener(OnWindowSettingsChanged);
            WindowSettingsPanel.gameObject.SetActive(false);
            Volume.SetActive(false);
            VolumeRenderingParent.SetActive(false);
            Slice2DView.gameObject.SetActive(false);
            SegmentConfiguration.transform.gameObject.SetActive(false);
            SegmentConfiguration.OnSelectionChanged2D.AddListener(SelectionChanged2D);
            SegmentConfiguration.OnSelectionChanged3D.AddListener(SelectionChanged3D);
            SegmentConfiguration.OnHideBaseChanged.AddListener(HideBaseChanged);

            Slice2DView.OnPointSelected.AddListener(SegmentConfiguration.UpdateRegionSeed);

            MainMenu.DisableButtons();
        }
	
        // Update is called once per frame
        private void Update ()
        {
            var progress = 0;
            var index = 0;

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

                    MainMenu.ProgressHandler.Max = 0;

                    break;
                }

                progress += tuple.Item1.Progress;
                index++;
            }

            MainMenu.ProgressHandler.Value = progress;
        }
        
        /// <summary>
        /// Window Settings have been changed.
        /// </summary>
        /// <param name="winWidth">new window width</param>
        /// <param name="winCenter">new window center</param>
        private void OnWindowSettingsChanged(double winWidth, double winCenter)
        {
            _stack.WindowWidth = winWidth;
            _stack.WindowCenter = winCenter;
        }

        /// <summary>
        /// Starts parsing the files in the currently selected folder.
        /// </summary>
        public void ParseFiles()
        {
            if (MainMenu.GetSelectedFolder() == MainMenu.FolderHint)
            {
                return;
            }

            _workIndicator.StartedWork();

            MainMenu.DisableDropDown();

            WindowSettingsPanel.DisableButtons();
            MainMenu.DisableButtons();
            WindowSettingsPanel.gameObject.SetActive(false);
            SegmentConfiguration.transform.gameObject.SetActive(false);
            AddWorkload(_stack.StartParsingFiles(Path.Combine(Application.streamingAssetsPath, MainMenu.GetSelectedFolder())),"Loading Files", OnFilesParsed);
        }

        /// <summary>
        /// Parsing of files has been completed.
        /// </summary>
        private void OnFilesParsed()
        {
            WindowSettingsPanel.Configure(_stack.MinPixelIntensity, _stack.MaxPixelIntensity, _stack.WindowWidthPresets, _stack.WindowCenterPresets);
            WindowSettingsPanel.gameObject.SetActive(true);
            _segmentCache.InitializeSize(_stack.Width, _stack.Height, _stack.Slices);
            SegmentConfiguration.Initialize(_segmentCache, _stack.MinPixelIntensity, _stack.MaxPixelIntensity);
            Slice2DView.Initialize(_stack);
            _workIndicator.FinishedWork();
            PreProcessData();
        }

        /// <summary>
        /// Starts preprocessing the stored DiFiles.
        /// </summary>
        public void PreProcessData()
        {
            _workIndicator.StartedWork();
            AddWorkload(_stack.StartPreprocessData(), "Preprocessing Data", OnPreProcessDone);
        }

        /// <summary>
        /// Preprocessing of the DiFiles is completed.
        /// </summary>
        private void OnPreProcessDone()
        {
            WindowSettingsPanel.EnableButtons();

            MainMenu.EnableButtons();
            MainMenu.EnableDropDown();

            _segmentCache.InitializeTextures();

            _workIndicator.FinishedWork();
            SegmentConfiguration.transform.gameObject.SetActive(true);
        }

        /// <summary>
        /// Starts creating a volume from the current data.
        /// </summary>
        public void CreateVolume()
        {
            _workIndicator.StartedWork();
            MainMenu.DisableButtons();
            WindowSettingsPanel.DisableButtons();
            AddWorkload(_stack.StartCreatingVolume(), "Creating Volume", OnVolumeCreated);
        }

        /// <summary>
        /// Volume creation is completed.
        /// </summary>
        private void OnVolumeCreated()
        {
            VolumeRendering.SetVolume(_stack.VolumeTexture);
            StartCoroutine(_segmentCache.ApplySegments(_stack.VolumeTexture, SegmentConfiguration.Display3Ds, SegmentConfiguration.HideBase));
            //RayMarching.initVolume(_stack.VolumeTexture);

            //Volume.SetActive(true);
            MainMenu.EnableButtons();
            WindowSettingsPanel.EnableButtons();
            _workIndicator.FinishedWork();
            VolumeRenderingParent.SetActive(true);
        }

        /// <summary>
        /// Starts creating 2D Textures for the current data.
        /// </summary>
        public void CreateTextures()
        {
            _workIndicator.StartedWork();
            MainMenu.DisableButtons();
            WindowSettingsPanel.DisableButtons();
            Slice2DView.gameObject.SetActive(true);
            AddWorkload(_stack.StartCreatingTextures(), "Creating Textures", OnTexturesCreated);
        }

        /// <summary>
        /// Texture creation has been completed.
        /// </summary>
        private void OnTexturesCreated()
        {
            _workIndicator.FinishedWork();
            MainMenu.EnableButtons();
            WindowSettingsPanel.EnableButtons();
            StartCoroutine(_segmentCache.ApplyTextures(SegmentConfiguration.Display2Ds, true));
        }

        /// <summary>
        /// Texture has been modified.
        /// </summary>
        /// <param name="type">SliceType of the texture</param>
        /// <param name="index">index of the texture</param>
        public void TextureUpdated(SliceType type, int index)
        {
            if (type == SliceType.Transversal && index == 50)
            {
                MainMenu.SetPreviewImage(_stack.GetTexture2D(type, index));
            }
        }

        /// <summary>
        /// A segment has been modified.
        /// </summary>
        /// <param name="selector">The selector for the modified segment</param>
        private void SegmentChanged(uint selector)
        {
            //combine selector with user selection and apply it to the cache.
            StartCoroutine(_segmentCache.ApplyTextures(SegmentConfiguration.Display2Ds, true));

            if (_stack.VolumeTexture)
            {
                CreateVolume();
            }
        }

        /// <summary>
        /// Value changed for visibility of base data
        /// </summary>
        /// <param name="hideBase">new state of visibility</param>
        private void HideBaseChanged(bool hideBase)
        {
            if (_stack.VolumeTexture)
            {
                CreateVolume();
            }
        }

        /// <summary>
        /// Visibility of a segment in 2D has changed
        /// </summary>
        /// <param name="selector">new selection of segments to display</param>
        private void SelectionChanged2D(uint selector)
        {
            StartCoroutine(_segmentCache.ApplyTextures(selector, true));
        }

        /// <summary>
        /// Visibility of a segment in 3D has changed
        /// </summary>
        /// <param name="selector">new selection of segments to display</param>
        private void SelectionChanged3D(uint selector)
        {
            CreateVolume();
        }

        /// <summary>
        /// Adds a workload to be completed.
        /// </summary>
        /// <param name="threadGroupState">State of the workload</param>
        /// <param name="description">Displayed description of the workload</param>
        /// <param name="onFinished">Callback for completed work</param>
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

        /// <summary>
        /// Removes the workload at the index and calls the callback
        /// </summary>
        /// <param name="index">index of the workload</param>
        private void RemoveWorkload(int index)
        {
            var tuple = _currentWorkloads[index];
            tuple.Item3.Invoke();
            _currentWorkloads.RemoveAt(index);
            _workIndicator.FinishedWork();
        }

    }
}
