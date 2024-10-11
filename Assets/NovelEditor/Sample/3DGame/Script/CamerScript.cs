using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public class CamerScript : MonoBehaviour
    {
        [SerializeField] Transform target;
        private Vector3 offset;
        // Start is called before the first frame update
        void Start()
        {
            offset = transform.position - target.position;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            transform.position = target.position + offset;
        }
    }
}
