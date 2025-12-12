using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Event
{
    [Serializable]
    public class TimelineController : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _playableDirector;

        // Enqueueの多重実行防止用フラグ
        private bool _isEnqueued = false;

        public void Enqueue()
        {
            if (_isEnqueued)
            {
                Debug.LogWarning($"TimelineController: Enqueue skipped (already enqueued) for {_playableDirector.name}", this);
                return;
            }
            _isEnqueued = true;
            Debug.Log($"TimelineController: Enqueueing timeline...{_playableDirector.name}", this);
            TimelineQueueManager.Instance.Enqueue(new TimelineTask(_playableDirector));
        }
        // Enqueue完了時に呼び出すことで再度Enqueue可能にする
        public void ResetEnqueueFlag()
        {
            _isEnqueued = false;
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