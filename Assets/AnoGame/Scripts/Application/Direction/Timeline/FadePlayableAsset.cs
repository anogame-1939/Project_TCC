using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    public enum FadeClipKind { In, Out, OutIn }

    [Serializable]
    public class FadePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public FadeClipKind kind = FadeClipKind.In;

        [Tooltip("true: クリップ長をフェード時間にする / false: 下のdurationを使う")]
        public bool useClipDuration = true;

        [Min(0f)] public float duration = 0.5f;
        public Color color = Color.black;

        // 連続再生やブレンドは不要なのでミニマム
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FadePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.kind = kind;
            behaviour.useClipDuration = useClipDuration;
            behaviour.durationOverride = duration;
            behaviour.color = color;
            return playable;
        }
    }
}
