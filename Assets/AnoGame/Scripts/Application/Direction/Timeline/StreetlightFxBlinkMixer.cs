using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.Gimmicks;

namespace AnoGame.Application.Direction.Timeline
{
    public class StreetlightFxBlinkMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fx = playerData as StreetlightFxBlink;
            if (fx == null) return;

            int inputCount = playable.GetInputCount();
            float blendedValue = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f) continue;

                var inputPlayable =
                    (ScriptPlayable<StreetlightFxCurveBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();

                double time = inputPlayable.GetTime();
                double duration = inputPlayable.GetDuration();
                float t = duration > 0.0001f ? (float)(time / duration) : 1f;
                t = Mathf.Clamp01(t);

                float curve01 = behaviour.intensityCurve.Evaluate(t);
                float clipValue = curve01; // or * behaviour.maxIntensity など

                blendedValue += clipValue * inputWeight;
                totalWeight += inputWeight;
            }

            if (totalWeight > 0f)
            {
                blendedValue /= totalWeight;
                fx.SetBlend(blendedValue);
            }
        }
    }
}
