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
        public void Start()
        {
            startEvent?.Invoke();
        }
    }
}