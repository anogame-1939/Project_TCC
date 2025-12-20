using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class EnemyResolveMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var proxy = playerData as TimelineEnemyResolveProxy;
            if (proxy == null) return;

            var controller = proxy.ResolveController;
            if (controller == null) return;

            int inputCount = playable.GetInputCount();
            float totalResolveWeight = 0f;
            float blendedResolve = 0f;

            EnemyResolveBehaviour bestParticleClip = null;
            float maxParticleWeight = -1f;
            float bestParticleT = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var playableInput = (ScriptPlayable<EnemyResolveBehaviour>)playable.GetInput(i);
                var behaviour = playableInput.GetBehaviour();

                double duration = playableInput.GetDuration();
                double time = playableInput.GetTime();
                float t = (duration > 0) ? (float)(time / duration) : 1f;
                t = Mathf.Clamp01(t);

                // --- Resolve Control ---
                if (behaviour.resolveControl)
                {
                    float curveT = behaviour.curve != null ? behaviour.curve.Evaluate(t) : t;
                    float currentAmount = Mathf.Lerp(behaviour.startAmount, behaviour.endAmount, curveT);
                    blendedResolve += currentAmount * weight;
                    totalResolveWeight += weight;
                }

                // --- Particle Control (Find winner) ---
                if (behaviour.particleControl && weight > maxParticleWeight)
                {
                    maxParticleWeight = weight;
                    bestParticleClip = behaviour;
                    bestParticleT = t;
                }
            }

            // Apply Resolve
            if (totalResolveWeight > 0f)
            {
                controller.SetResolve(blendedResolve);
            }

            // Apply Particle Logic
            if (bestParticleClip != null)
            {
                // 色を適用
                controller.SetParticleColor(bestParticleClip.particleColor);

                switch (bestParticleClip.particleAction)
                {
                    case ParticleAction.PlayAtThreshold:
                        controller.HandleEffectByProgress(bestParticleT, bestParticleClip.playThreshold, bestParticleClip.stopThreshold);
                        break;
                    case ParticleAction.ForcePlay:
                        controller.PlayEffect();
                        break;
                    case ParticleAction.ForceStop:
                        controller.StopEffect();
                        break;
                }

                // クリップ終了時の停止処理
                if (bestParticleClip.stopAtClipEnd && bestParticleT >= 1f - Time.deltaTime) // ほぼ終了
                {
                    controller.StopEffect();
                }
            }
        }
    }
}
