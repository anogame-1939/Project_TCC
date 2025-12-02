using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.GameCamera;

namespace AnoGame.Application.Timeline.CameraShake
{
    public class CameraShakeMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var controller = playerData as CinemachineShakeController;
            if (controller == null) return;

            float totalIntensity = 0f;
            float totalFrequency = 0f;

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight > 0f)
                {
                    ScriptPlayable<CameraShakeBehaviour> inputPlayable = (ScriptPlayable<CameraShakeBehaviour>)playable.GetInput(i);
                    CameraShakeBehaviour input = inputPlayable.GetBehaviour();

                    totalIntensity += input.intensity * inputWeight;
                    totalFrequency += input.frequency * inputWeight;
                }
            }

            controller.SetShake(totalIntensity, totalFrequency);
        }
    }
}
