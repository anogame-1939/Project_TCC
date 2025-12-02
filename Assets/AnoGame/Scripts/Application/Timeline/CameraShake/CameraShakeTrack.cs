using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using AnoGame.Application.GameCamera;

namespace AnoGame.Application.Timeline.CameraShake
{
    [TrackColor(0.8f, 0.2f, 0.2f)]
    [TrackClipType(typeof(CameraShakeClip))]
    [TrackBindingType(typeof(CinemachineShakeController))]
    public class CameraShakeTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CameraShakeMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
