using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.Gimmicks;

namespace AnoGame.Application.Direction.Timeline
{
    public class StreetlightFxBlinkBehaviour : PlayableBehaviour
    {
        [Range(0f, 1f)]
        public float from = 0f;

        [Range(0f, 1f)]
        public float to = 1f;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var fx = playerData as StreetlightFxBlink;
            if (fx == null) return;

            double time = playable.GetTime();
            double duration = playable.GetDuration();
            float normalizedTime = duration > 0.0001
                ? (float)(time / duration)
                : 1f;

            float blend = Mathf.Lerp(from, to, normalizedTime);
            fx.SetBlend(blend);
        }
    }
}
