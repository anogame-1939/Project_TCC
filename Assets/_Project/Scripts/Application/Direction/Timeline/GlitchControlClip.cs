using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [System.Serializable]
    public sealed class GlitchControlClip : PlayableAsset, ITimelineClipAsset
    {
        public GlitchControlBehaviour template = new GlitchControlBehaviour();

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
            => ScriptPlayable<GlitchControlBehaviour>.Create(graph, template);

        public ClipCaps clipCaps => ClipCaps.None;
    }
}
