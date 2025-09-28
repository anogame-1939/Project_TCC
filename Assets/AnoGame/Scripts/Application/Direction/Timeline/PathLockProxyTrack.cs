using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackClipType(typeof(PathLockPlayableAsset))]            // 既存クリップを再利用
    [TrackBindingType(typeof(TimelineEventLockProxy))]        // ★ ここがポイント
    public sealed class PathLockProxyTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // 既存ミキサー流用 or 新規ミキサー。ここでは例として新規:
            var playable = ScriptPlayable<PathLockProxyMixer>.Create(graph, inputCount);
            return playable;
        }
    }
}