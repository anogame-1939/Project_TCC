using System;
using AnoGame.Application;
using UnityEngine;
using UnityEngine.Playables;
namespace AnoGame.AnoFlow
{
    [RequireComponent(typeof(PlayableDirector))]
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

            bool canSkip = OnSkip != null && OnSkip.GetPersistentEventCount() > 0;
            TimelineQueueManager.Instance.Enqueue(new TimelineTask(_playableDirector, ResetEnqueueFlag, canSkip));
        }

        [Header("スキップ時に強制適用する状態変更処理")]
        public UnityEngine.Events.UnityEvent OnSkip;

        /// <summary>
        /// スキップ実行。登録されたイベントを発火させる。
        /// </summary>
        public void PerformSkip()
        {
            Debug.Log($"TimelineController: PerformSkip for {_playableDirector.name}", this);
            OnSkip?.Invoke();
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