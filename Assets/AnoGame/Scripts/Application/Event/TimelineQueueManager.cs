using System;
using System.Collections.Generic;
using AnoGame.Application.Core;
using UniRx;
using UnityEngine.Events;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// タイムラインをキューで順番に再生するマネージャー
    /// </summary>
    public class TimelineQueueManager : SingletonMonoBehaviour<TimelineQueueManager>
    {
        private readonly Queue<TimelineTask> _queue = new();
        private bool _isPlaying;

        // 追加: 「今シリーズ中かどうか」
        private bool _sequenceActive;

        // 追加: 外からも差し込めるようにしておくと便利
        public UnityEvent OnSequenceStarted;
        public UnityEvent OnSequenceFinished;

        /// <summary>
        /// タイムラインをキューに追加する。再生中でなければすぐ再生。
        /// </summary>
        public void Enqueue(TimelineTask task)
        {
            if (task == null) return;

            _queue.Enqueue(task);

            // 何も再生していなければ今すぐ再生
            if (!_isPlaying)
            {
                // ←ここが「シリーズ開始」のタイミング
                StartSequenceIfNeeded();
                PlayNext();
            }
        }

        private void PlayNext()
        {
            // もうキューが空なら終了処理
            if (_queue.Count == 0)
            {
                _isPlaying = false;
                EndSequenceIfNeeded();
                return;
            }

            _isPlaying = true;

            var task = _queue.Dequeue();

            // タスクが終わったら次へ
            task.OnCompleted += () =>
            {
                _isPlaying = false;
                PlayNext();
            };

            task.Play();
        }

        /// <summary>
        /// 今溜まってるやつ全部消したいとき
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
            _isPlaying = false;

            // キャンセルとして扱うならシリーズも終わらせる
            EndSequenceIfNeeded();
        }

        private void StartSequenceIfNeeded()
        {
            if (_sequenceActive) return;

            _sequenceActive = true;
            OnSequenceStarted?.Invoke();
        }

        private void EndSequenceIfNeeded()
        {
            if (!_sequenceActive) return;

            _sequenceActive = false;
            OnSequenceFinished?.Invoke();
        }
    }
}
