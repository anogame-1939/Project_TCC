using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [Serializable]
    public class StreetlightFxCurveClip : PlayableAsset, ITimelineClipAsset
    {
        public StreetlightFxCurveBehaviour template = new StreetlightFxCurveBehaviour();

        public ClipCaps clipCaps =>
            ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Looping;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<StreetlightFxCurveBehaviour>.Create(graph, template);
            return playable;
        }
    }
}
