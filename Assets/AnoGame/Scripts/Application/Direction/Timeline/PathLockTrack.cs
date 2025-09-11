using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnoGame.Application.Player.Control; // EventLockControl の名前空間

[TrackColor(0.3f, 0.8f, 1f)]
[TrackClipType(typeof(PathLockPlayableAsset))]
[TrackBindingType(typeof(EventLockControl))] // バインド先：EventLockControl
public class PathLockTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        => ScriptPlayable<PathLockMixer>.Create(graph, inputCount);
}