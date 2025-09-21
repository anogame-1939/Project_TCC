using AnoGame.Application.Direction.Glitch;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnoGame.Application.Direction.Timeline
{
    [TrackColor(0.7f, 0.2f, 1f)]
    [TrackClipType(typeof(GlitchControlClip))]
    [TrackBindingType(typeof(GlitchControllerProxy))]
    public sealed class GlitchControlTrack : TrackAsset
    {
    [SerializeField] bool resetToZeroWhenNoClip = true;   // クリップが無いフレームで 0 に戻す
    [SerializeField] bool disableWhenNoClip     = false;  // ついでに機能自体もOFFにするか（任意）

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var playable = ScriptPlayable<GlitchControlMixer>.Create(graph, inputCount);
        var m = playable.GetBehaviour();
        m.resetToZeroWhenNoClip = resetToZeroWhenNoClip;
        m.disableWhenNoClip     = disableWhenNoClip;
        return playable;
    }
    }
}
