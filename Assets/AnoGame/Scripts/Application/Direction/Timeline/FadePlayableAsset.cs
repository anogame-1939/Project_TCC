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

        public Color color = Color.black;

        [Tooltip("クリップ終了時にフェード状態を維持するか")]
        public bool keepFadeState = true;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FadePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.kind = kind;
            behaviour.color = color;
            behaviour.keepFadeState = keepFadeState;
            return playable;
        }
    }
}
