using AnoGame.Application.Direction.Glitch;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class GlitchControlMixer : PlayableBehaviour
    {
        // 今回は“最後に入ってきたクリップを適用”の簡易仕様
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var proxy = playerData as GlitchControllerProxy;
            if (proxy == null || proxy.Controller == null) return;

            int clipCount = playable.GetInputCount();
            for (int i = 0; i < clipCount; i++)
            {
                var input = (ScriptPlayable<GlitchControlBehaviour>)playable.GetInput(i);
                var weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var behaviour = input.GetBehaviour();
                behaviour.Apply(proxy.Controller);
            }
        }
    }
}
