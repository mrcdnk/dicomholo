using System;
using System.Collections.Generic;
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
            _stack = gameObject.AddComponent<ImageStack>();
            _stack.MainMenu = MainMenu;
            _stack.Selection = MainMenu.Selection;
            _stack.ViewManager = this;

            Slice2DView.ImageStack = _stack;

            Volume.SetActive(false);
            Slice2DView.gameObject.SetActive(false);
            InitFiles();
        }
	
        // Update is called once per frame
        void Update ()
        {
            int progress = 0;
            int index = 0;

            while (_currentWorkloads.Count > 0 && index < _currentWorkloads.Count)
            {
                var tuple = _currentWorkloads[index];
                if (tuple.Item1.progress == tuple.Item1.TotalProgress && tuple.Item1.working == 0)
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

                progress += tuple.Item1.progress;
                index++;
            }

            MainMenu.ProgressHandler.Value = progress;
        }

        public void InitFiles()
        {
            MainMenu.DisableButtons();
            AddWorkload(_stack.StartInitFiles(),"Loading Files", () =>
            {
                Slice2DView.InitSlider();
                PreProcessData();
            });
        }

        public void PreProcessData()
        {
            AddWorkload(_stack.StartPreprocessData(), "Preprocessing Data", () =>
            {
                MainMenu.EnableButtons();
            });
        }

        public void CreateVolume()
        {
            MainMenu.LoadVolumeButton.enabled = false;
            AddWorkload(_stack.StartCreatingVolume(), "Creating Volume", () => {
                VolumeRendering.SetVolume(_stack.Texture3D);
                //RayMarching.initVolume(_stack.Texture3D);

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
