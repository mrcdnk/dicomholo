using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VolumeRendering
{

    public class Axis : MonoBehaviour {

        [SerializeField] protected Transform root;
        [SerializeField] protected float length = 5f;

        protected void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.Rotate(root.rotation) * Matrix4x4.Rotate(transform.rotation).inverse;

            Vector3 pos = transform.position;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos+ Vector3.right * length);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, pos+Vector3.up * length);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos+Vector3.forward * length);
        }

    }

}


