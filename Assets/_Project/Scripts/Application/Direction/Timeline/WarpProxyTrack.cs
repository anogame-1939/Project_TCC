using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.95f, 0.70f, 0.20f)]                         // 任意：見分けやすく
    [TrackClipType(typeof(WarpPlayableAsset))]                // ← Warp 用クリップ
    [TrackBindingType(typeof(TimelineEventLockProxy))]        // ← バインド先は Proxy
    public sealed class WarpProxyTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // Warp は一発モノが多いのでミキサーは軽量でOK
            return ScriptPlayable<WarpProxyMixer>.Create(graph, inputCount);
        }
    }
}
