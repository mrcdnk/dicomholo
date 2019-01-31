using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.UX;
using UnityEngine;

namespace DICOMViews
{
    [RequireComponent(typeof(BoundingBoxRig))]
    public class VolumeInteraction : MonoBehaviour
    {
        private BoundingBoxRig _boundingBoxRig;
        private AppBar _appBar;

        // Start is called before the first frame update
        private void Start()
        {
            _boundingBoxRig = GetComponent<BoundingBoxRig>();

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}

