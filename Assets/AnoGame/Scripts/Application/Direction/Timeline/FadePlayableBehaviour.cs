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
        public bool keepFadeState;

        private FadeHandler _handler;

        private bool _fired;

        public override void OnGraphStart(Playable playable)
        {
            _fired = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _handler = playerData as FadeHandler;
            if (_handler == null) return;

            if (!_fired && info.effectivePlayState == PlayState.Playing)
            {
                _fired = true;

                float duration = useClipDuration ? (float)playable.GetDuration() : durationOverride;
                if (duration < 0f) duration = 0f;

                switch (kind)
                {
                    case FadeClipKind.In: _handler.FadeIn(duration, color); break;
                    case FadeClipKind.Out: _handler.FadeOut(duration, color); break;
                    case FadeClipKind.OutIn: _handler.FadeOutIn(duration, color); break;
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (keepFadeState && _handler != null)
            {
                var duration = playable.GetDuration();
                var time = playable.GetTime();
                if (time >= duration - 0.001f)
                {
                    switch (kind)
                    {
                        case FadeClipKind.In: _handler.FadeIn(0, color); break;
                        case FadeClipKind.Out: _handler.FadeOut(0, color); break;
                        case FadeClipKind.OutIn: _handler.FadeIn(0, color); break;
                    }
                }
            }
            _fired = false; // 逆再生やスクラブで再入可能に
        }
    }

}
