using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.Application.Gimmicks;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.9f, 0.9f, 0.3f)]
    [TrackBindingType(typeof(Light))]
    [TrackClipType(typeof(StreetlightFxCurveClip))]
    public class StreetlightFxCurveTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<StreetlightFxCurveMixerBehaviour>.Create(graph, inputCount);
        }
    }

}
