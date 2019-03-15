using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.UX.Progress;
using UnityEngine;

public class GlobalWorkIndicator : MonoBehaviour
{
    private int _semaphore = 0;

    private Animator _cursorAnimator;

    // Start is called before the first frame update
    void Start()
    {
        _cursorAnimator = GameObject.FindGameObjectWithTag("HoloCursor").GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartedWork()
    {
        if (!_cursorAnimator)
        {
            return;
        }

        _semaphore++;
        if (_semaphore == 1)
        {
            //_progressIndicator.gameObject.SetActive(true);
            _cursorAnimator.SetBool("Waiting", true);
        }
    }

    public void FinishedWork()
    {
        if (!_cursorAnimator)
        {
            return;
        }

        if (_semaphore > 0)
        {
            _semaphore--;
        }

        if (_semaphore == 0)
        {
            //_progressIndicator.gameObject.SetActive(false);
            _cursorAnimator.SetBool("Waiting", false);
        }
    }
}
