using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    /// <summary>
    /// ProcessFrame 駆動のフェード Behaviour。
    /// Timeline のクリップ再生位置から毎フレーム alpha を計算し、
    /// FadeHandler 経由で直接 FadeImage.Range を設定する。
    /// コルーチンに依存しないため、クリップ長と完全に同期する。
    /// </summary>
    public class FadePlayableBehaviour : PlayableBehaviour
    {
        public FadeClipKind kind;
        public Color color = Color.black;
        public bool keepFadeState;

        private FadeHandler _handler;
        private bool _initialized;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _handler = playerData as FadeHandler;
            if (_handler == null) return;
            if (!UnityEngine.Application.isPlaying) return;

            _initialized = true;

            double time = playable.GetTime();
            double clipDuration = playable.GetDuration();

            // 安全ガード
            if (clipDuration <= 0) clipDuration = 0.001;

            float t = Mathf.Clamp01((float)(time / clipDuration));

            float range;
            switch (kind)
            {
                case FadeClipKind.In:
                    // FadeIn: 暗転(1) → 透明(0)
                    range = 1f - t;
                    break;

                case FadeClipKind.Out:
                    // FadeOut: 透明(0) → 暗転(1)
                    range = t;
                    break;

                case FadeClipKind.OutIn:
                    // FadeOutIn: 前半 0→1、後半 1→0
                    if (t <= 0.5f)
                    {
                        range = t * 2f; // 0→1
                    }
                    else
                    {
                        range = (1f - t) * 2f; // 1→0
                    }
                    break;

                default:
                    range = 0f;
                    break;
            }

            _handler.SetFadeRange(range, color);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_initialized || _handler == null || !UnityEngine.Application.isPlaying) return;

            double time = playable.GetTime();
            double clipDuration = playable.GetDuration();

            // クリップが終端に達した場合
            bool clipEnded = time >= clipDuration - 0.05;

            if (clipEnded)
            {
                if (keepFadeState)
                {
                    // 最終状態を維持
                    float endRange;
                    switch (kind)
                    {
                        case FadeClipKind.In: endRange = 0f; break; // 透明
                        case FadeClipKind.Out: endRange = 1f; break; // 暗転
                        case FadeClipKind.OutIn: endRange = 0f; break; // 透明に戻る
                        default: endRange = 0f; break;
                    }
                    _handler.SetFadeRange(endRange, color);
                }
                // keepFadeState=false の場合、最終フレームの値のまま残る
            }

            _initialized = false;
        }
    }
}
