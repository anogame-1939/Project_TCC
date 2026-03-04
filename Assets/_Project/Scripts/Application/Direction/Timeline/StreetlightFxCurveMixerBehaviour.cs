using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.Gimmicks;

namespace AnoGame.Application.Direction.Timeline
{
    public class StreetlightFxCurveMixerBehaviour : PlayableBehaviour
    {
        private bool _initialized;
        private float _originalIntensity;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var light = playerData as Light;
            if (light == null)
                return;

            if (!_initialized)
            {
                _originalIntensity = light.intensity;
                _initialized = true;
            }

            int inputCount = playable.GetInputCount();

            float blendedIntensity = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                var inputPlayable =
                    (ScriptPlayable<StreetlightFxCurveBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();

                double time = inputPlayable.GetTime();
                double duration = inputPlayable.GetDuration();
                float tNorm = duration > 0.0 ? (float)(time / duration) : 0f;
                tNorm = Mathf.Clamp01(tNorm);

                float curve01 = behaviour.intensityCurve != null
                    ? behaviour.intensityCurve.Evaluate(tNorm)
                    : 1f;

                float intensity = behaviour.maxIntensity * curve01;

                blendedIntensity += intensity * inputWeight;
                totalWeight += inputWeight;
            }

            // 何もクリップが効いていない部分は元の明るさを残す
            if (totalWeight > 0f)
                light.intensity = blendedIntensity + _originalIntensity * (1f - totalWeight);
            else
                light.intensity = _originalIntensity;
        }

        public override void OnGraphStop(Playable playable)
        {
            _initialized = false;
        }
    }

}
