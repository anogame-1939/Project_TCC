using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.AnoFlow
{
    [Serializable]
    public class TimelineTask : ITimelineTask
    {
        public PlayableDirector Director { get; private set; }
        public bool IsSkipAvailable { get; private set; }
        public Action OnCompleted { get; set; }  // 終了時に呼ばれる任意のコールバック

        public TimelineTask(PlayableDirector director, Action onCompleted = null, bool isSkipAvailable = false)
        {
            Director = director;
            OnCompleted = onCompleted;
            IsSkipAvailable = isSkipAvailable;
        }

        /// <summary>
        /// 実際に再生を開始する。終わったらコールバックを呼ぶ。
        /// </summary>
        public void Play()
        {
            if (Director == null)
            {
                Debug.LogWarning("TimelineTask: Director is null.");
                OnCompleted?.Invoke();
                return;
            }

            // 念のため頭から再生
            Director.time = 0;
            Director.stopped -= HandleStopped; // 二重登録防止
            Director.stopped += HandleStopped;
            Director.Play();
        }

        private void HandleStopped(PlayableDirector d)
        {
            d.stopped -= HandleStopped;
            OnCompleted?.Invoke();
        }
    }
}