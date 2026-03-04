using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.95f, 0.20f, 0.20f)]                         // 任意：敵なので赤っぽく
    [TrackClipType(typeof(WarpPlayableAsset))]                // ← Warp 用クリップ (既存のものを再利用)
    [TrackBindingType(typeof(TimelineEnemyEventLockProxy))]   // ← バインド先は EnemyProxy
    public sealed class WarpEnemyProxyTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<WarpEnemyProxyMixer>.Create(graph, inputCount);
        }
    }
}
