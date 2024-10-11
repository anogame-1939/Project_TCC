using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public abstract class SpeekableObject : MonoBehaviour
    {
        public abstract NovelData GetNovelData();
        private GameObject canvas;
        public bool canSpeak { get; private set; }

        protected void Init()
        {
            var ui = Resources.Load<GameObject>("DialogueCanvas"); ;

            canvas = GameObject.Instantiate(ui);
            canvas.transform.SetParent(transform);
            canvas.transform.localPosition = Vector3.zero;
            canvas.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            canvas.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            canvas.SetActive(false);
        }
    }
}
