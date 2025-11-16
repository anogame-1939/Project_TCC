using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [System.Serializable]
    public class StreetlightFxBlinkClip : PlayableAsset, ITimelineClipAsset
    {
        [Range(0f, 1f)]
        public float from = 0f;

        [Range(0f, 1f)]
        public float to = 1f;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<StreetlightFxBlinkBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.from = from;
            behaviour.to = to;
            return playable;
        }
    }
}
