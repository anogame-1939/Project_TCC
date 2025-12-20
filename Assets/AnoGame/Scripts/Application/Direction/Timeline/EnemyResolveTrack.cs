using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.2f, 0.5f, 0.8f)]
    [TrackClipType(typeof(EnemyResolveClip))]
    [TrackBindingType(typeof(TimelineEnemyResolveProxy))]
    public sealed class EnemyResolveTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<EnemyResolveMixer>.Create(graph, inputCount);
        }
    }
}
