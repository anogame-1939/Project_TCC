using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.Application.Player.Control;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.95f, 0.7f, 0.2f)]
    [TrackClipType(typeof(WarpPlayableAsset))]
    [TrackBindingType(typeof(EventLockControl))] // ★ EventLockControl をバインド
    public class WarpTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<WarpMixer>.Create(graph, inputCount);
    }
}
