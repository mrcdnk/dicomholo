using System.Collections;
using System.Collections.Generic;
using HoloToolkit.UX.Progress;
using UnityEngine;

public class GlobalWorkIndicator : MonoBehaviour
{
    private int semaphore = 0;

    private ProgressIndicatorOrbsRotator _progressIndicator;

    // Start is called before the first frame update
    void Start()
    {
        _progressIndicator = GetComponentInChildren<ProgressIndicatorOrbsRotator>();
        _progressIndicator.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartedWork()
    {
        if (!_progressIndicator)
        {
            return;
        }

        semaphore++;
        if (semaphore == 1)
        {
            _progressIndicator.gameObject.SetActive(true);
        }
    }

    public void FinishedWork()
    {
        if (!_progressIndicator)
        {
            return;
        }

        if (semaphore > 0)
        {
            semaphore--;
        }

        if (semaphore == 0)
        {
            _progressIndicator.gameObject.SetActive(false);
        }
    }
}
