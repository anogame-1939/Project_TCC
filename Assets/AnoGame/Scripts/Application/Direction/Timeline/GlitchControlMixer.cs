using AnoGame.Application.Direction.Glitch;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class GlitchControlMixer : PlayableBehaviour
    {
        // 今回は“最後に入ってきたクリップを適用”の簡易仕様
public override void ProcessFrame(Playable playable, FrameData info, object playerData)
{
    var proxy = playerData as GlitchControllerProxy;
    if (proxy == null || proxy.Controller == null) {
        Debug.LogWarning("[GLITCH_TIMELINE] No binding or controller.", proxy);
        return;
    }

    int clipCount = playable.GetInputCount();
    double time = playable.GetTime();

    for (int i = 0; i < clipCount; i++)
    {
        var input = (ScriptPlayable<GlitchControlBehaviour>)playable.GetInput(i);
        float weight = playable.GetInputWeight(i);
        if (weight <= 0f) continue;

        var behaviour = input.GetBehaviour();
        Debug.Log($"[GLITCH_TIMELINE] ProcessFrame clip#{i} w={weight:0.00} t={time:0.00}", proxy);
        behaviour.Apply(proxy.Controller);
    }
}
    }
}
