using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class EnemyResolveClip : PlayableAsset, ITimelineClipAsset
    {
        public EnemyResolveBehaviour template = new EnemyResolveBehaviour();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<EnemyResolveBehaviour>.Create(graph, template);
        }
    }
}
