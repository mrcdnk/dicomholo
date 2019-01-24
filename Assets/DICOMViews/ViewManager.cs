using System;
using System.Collections.Generic;
using System.IO;
using DICOMParser;
using Threads;
using UnityEngine;

namespace DICOMViews
{
    public class ViewManager : MonoBehaviour
    {
        public MainMenu MainMenu;
        public WindowSettingsPanel WindowSettingsPanel;
        public Slice2DView Slice2DView;

        public VolumeRendering.VolumeRendering VolumeRendering;

        public GameObject Volume;
        public RayMarching RayMarching;

        private ImageStack _stack;
        private readonly List<Tuple<ThreadGroupState, string, Action>> _currentWorkloads = new List<Tuple<ThreadGroupState, string, Action>>(5);


        // Use this for initialization
        void Start ()
        {
            MainMenu.Selection.ClearOptions();

            var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

            var names = new List<string>();

            foreach (var fold in folders)
            {
                names.Add(fold.Split('\\')[1]);

            }

            MainMenu.Selection.AddOptions(names);

            _stack = gameObject.AddComponent<ImageStack>();
            _stack.Selection = MainMenu.Selection;
            _stack.ViewManager = this;

            Slice2DView.ImageStack = _stack;
            WindowSettingsPanel.SettingsChangedEvent.AddListener(OnWindowSettingsChanged);

            WindowSettingsPanel.gameObject.SetActive(false);
            Volume.SetActive(false);
            Slice2DView.gameObject.SetActive(false);

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
            AddWorkload(_stack.StartParsingFiles(),"Loading Files", OnFilesParsed);
        }

        private void OnFilesParsed()
        {
            WindowSettingsPanel.Configure(_stack.MinPixelIntensity, _stack.MaxPixelIntensity, _stack.WindowWidth, _stack.WindowCenter);
            WindowSettingsPanel.gameObject.SetActive(true);
            Slice2DView.InitSlider();
            PreProcessData();
        }

        public void PreProcessData()
        {
            AddWorkload(_stack.StartPreprocessData(), "Preprocessing Data", () =>
            {
                WindowSettingsPanel.EnableButtons();
                MainMenu.EnableButtons();
            });
        }

        public void CreateVolume()
        {
            MainMenu.LoadVolumeButton.enabled = false;
            AddWorkload(_stack.StartCreatingVolume(), "Creating Volume", () => {
                VolumeRendering.SetVolume(_stack.Texture3D);
                RayMarching.initVolume(_stack.Texture3D);

                Volume.SetActive(true);
                MainMenu.LoadVolumeButton.enabled = true;
            });
        }

        public void CreateTextures()
        {
            MainMenu.Load2DButton.enabled = false;
            Slice2DView.gameObject.SetActive(true);
            AddWorkload(_stack.StartCreatingTextures(), "Creating Textures", () => {
                MainMenu.Load2DButton.enabled = true;
            });
        }

        public void TextureUpdated(SliceType type, int index)
        {
            Slice2DView.TextureUpdated(type, index);

            if (type == SliceType.Transversal && index == 50)
            {
                MainMenu.SetPreviewImage(_stack.GetTexture2D(type, index));
            }
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
