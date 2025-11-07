using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Event
{
    [Serializable]
    public class TimelineController : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _playableDirector;

        public void Enqueue()
        {
            Debug.Log("TimelineController: Enqueueing timeline.", this);
            TimelineQueueManager.Instance.Enqueue(new TimelineTask(_playableDirector));
        }

        private void OnValidate()
        {
            if (_playableDirector == null)
            {
                _playableDirector = GetComponent<PlayableDirector>();
            }
        }
    }
}