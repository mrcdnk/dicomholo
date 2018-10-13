using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class InitFileSelector : MonoBehaviour {

    public Dropdown dropdown;

	// Use this for initialization
	void Start () {
        dropdown.ClearOptions();
        Debug.Log(System.AppDomain.CurrentDomain.BaseDirectory);

        var folders = new List<string>(Directory.GetDirectories(Application.streamingAssetsPath));

        var names = new List<string>();

        foreach (var fold in folders)
        {
            names.Add(fold.Split('\\')[1]);

        }
     
        dropdown.AddOptions(names);


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
