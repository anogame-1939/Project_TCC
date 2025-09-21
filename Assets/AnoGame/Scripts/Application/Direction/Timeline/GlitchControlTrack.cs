using AnoGame.Application.Direction.Glitch;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.7f, 0.2f, 1f)]
    [TrackClipType(typeof(GlitchControlClip))]
    [TrackBindingType(typeof(GlitchControllerProxy))]
    public sealed class GlitchControlTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<GlitchControlMixer>.Create(graph, inputCount);
    }
}
