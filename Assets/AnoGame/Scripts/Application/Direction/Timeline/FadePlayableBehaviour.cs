using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public class FadePlayableBehaviour : PlayableBehaviour
    {
        public FadeClipKind kind;
        public bool useClipDuration = true;
        public float durationOverride = 0.5f;
        public Color color = Color.black;

        private bool _fired;

        public override void OnGraphStart(Playable playable)
        {
            _fired = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var handler = playerData as FadeHandler;
            if (handler == null) return;

            if (!_fired && info.effectivePlayState == PlayState.Playing)
            {
                _fired = true;

                float duration = useClipDuration ? (float)playable.GetDuration() : durationOverride;
                if (duration < 0f) duration = 0f;

                switch (kind)
                {
                    case FadeClipKind.In: handler.FadeIn(duration, color); break;
                    case FadeClipKind.Out: handler.FadeOut(duration, color); break;
                    case FadeClipKind.OutIn: handler.FadeOutIn(duration, color); break;
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _fired = false; // 逆再生やスクラブで再入可能に
        }
    }

}
