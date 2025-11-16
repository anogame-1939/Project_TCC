using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{

    [Serializable]
    public class StreetlightFxCurveBehaviour : PlayableBehaviour
    {
        [Tooltip("0〜1の値を返すカーブ。0=消灯, 1=最大輝度")]
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip("このクリップで使う基準の明るさ")]
        public float maxIntensity = 1f;

        // 必要なら「クリップ開始前の値を戻す」ために退避
        private Light _boundLight;
        private float _originalIntensity;
        private bool _initialized;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Debug.Log("" + info);
            var light = playerData as Light;
            if (light == null)
                return;

            if (!_initialized)
            {
                _boundLight = light;
                _originalIntensity = light.intensity;
                _initialized = true;
            }

            double time = playable.GetTime();
            double duration = playable.GetDuration();
            float tNorm = duration > 0.0 ? (float)(time / duration) : 0f;
            tNorm = Mathf.Clamp01(tNorm);

            float curve01 = intensityCurve != null ? intensityCurve.Evaluate(tNorm) : 1f;
            light.intensity = maxIntensity * curve01;
        }

        public override void OnGraphStop(Playable playable)
        {
            // クリップ終了時に元の明るさに戻したい場合
            if (_boundLight != null)
            {
                _boundLight.intensity = _originalIntensity;
            }
            _initialized = false;
        }
    }

}
