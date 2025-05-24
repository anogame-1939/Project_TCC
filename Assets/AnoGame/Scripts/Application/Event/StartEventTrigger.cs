using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// スタート時に必ずイベント実行したいだけ
    /// </summary>
    public class StartEventTrigger : MonoBehaviour
    {
        [SerializeField] UnityEvent startEvent;
        [SerializeField] float delay = 0f;
        public void Start()
        {
            StartCoroutine(StartCoroutine());
        }

        private IEnumerator StartCoroutine()
        {
            yield return new WaitForSeconds(delay);
            startEvent?.Invoke();
        }
    }
}