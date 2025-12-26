using System.Collections.Generic;
using AnoGame.Application.Core;
using UnityEngine.Events;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// タイムラインをキューで順番に再生するマネージャー
    /// </summary>
    public class TimelineQueueManager : SingletonMonoBehaviour<TimelineQueueManager>
    {
        private readonly Queue<ITimelineTask> _queue = new();
        private bool _isPlaying;

        // 追加: 「今シリーズ中かどうか」
        private bool _sequenceActive;
        private bool _isSkipping;

        private ITimelineTask _currentTask;

        // 追加: 外からも差し込めるようにしておくと便利
        public UnityEvent OnSequenceStarted;
        public UnityEvent OnSequenceFinished;

        /// <summary>
        /// タイムラインをキューに追加する。再生中でなければすぐ再生。
        /// </summary>
        public void Enqueue(ITimelineTask task)
        {
            if (task == null) return;

            _queue.Enqueue(task);

            // 何も再生していなければ今すぐ再生
            if (!_isPlaying && !_isSkipping)
            {
                // ←ここが「シリーズ開始」のタイミング
                StartSequenceIfNeeded();
                PlayNext();
            }
        }

        private void PlayNext()
        {
            if (_isSkipping) return;

            // もうキューが空なら終了処理
            if (_queue.Count == 0)
            {
                _isPlaying = false;
                EndSequenceIfNeeded();
                return;
            }

            _isPlaying = true;

            _currentTask = _queue.Dequeue();

            // タスクが終わったら次へ
            _currentTask.OnCompleted += () =>
            {
                UnityEngine.Debug.Log("Task completed.");
                _isPlaying = false;
                _currentTask = null;
                PlayNext();
            };

            UnityEngine.Debug.Log("Playing task..." + _currentTask.GetType().Name);

            _currentTask.Play();
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

        public void SkipCurrentSequence()
        {
            if (!_sequenceActive || _isSkipping) return;
            StartCoroutine(SkipCoroutine());
        }

        private System.Collections.IEnumerator SkipCoroutine()
        {
            _isSkipping = true;

            // フェードアウト
            const float fadeTime = 0.5f;
            AnoGame.Application.UI.FadeManager.Instance.FadeOut(fadeTime);
            yield return new UnityEngine.WaitForSeconds(fadeTime);

            // まず状態強制を適用
            // 現在実行中のものも含めて、ControllerがあればSkip実行
            if (_currentTask is TimelineTask t && t.Director != null)
            {
                var controller = t.Director.GetComponent<TimelineController>();
                if (controller != null)
                {
                    controller.PerformSkip();
                }
                t.Director.Stop(); // これでPlayNextが呼ばれるが、isSkippingで弾かれる
            }
            _currentTask = null;

            // キューに残っているものも同様に処理
            while (_queue.Count > 0)
            {
                var task = _queue.Dequeue();
                if (task is TimelineTask tt && tt.Director != null)
                {
                    var c = tt.Director.GetComponent<TimelineController>();
                    if (c != null)
                    {
                        c.PerformSkip();
                    }
                }
                // 再生せずに飛ばすので、OnCompleted（ResetEnqueueFlagなど）は手動で呼ぶ
                task.OnCompleted?.Invoke();
            }

            // フェードイン
            yield return null; // 1フレ待って状態反映を確実にする
            AnoGame.Application.UI.FadeManager.Instance.FadeIn(fadeTime);

            // シリーズ終了
            _isPlaying = false;
            _isSkipping = false;
            EndSequenceIfNeeded();
        }
    }
}
