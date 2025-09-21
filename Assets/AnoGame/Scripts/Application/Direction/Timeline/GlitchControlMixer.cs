using AnoGame.Application.Direction.Glitch;
using UnityEngine;
using UnityEngine.Playables;

namespace AnoGame.Application.Direction.Timeline
{
    public sealed class GlitchControlMixer : PlayableBehaviour
    {
        [HideInInspector] public bool resetToZeroWhenNoClip = true;
        [HideInInspector] public bool disableWhenNoClip     = false;

        // 停止時リセット用に最後の Proxy を覚えておく
        GlitchControllerProxy _lastProxy;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var proxy = playerData as GlitchControllerProxy;
            if (proxy == null || proxy.Controller == null) return;
            _lastProxy = proxy;

            int clipCount = playable.GetInputCount();
            float totalWeight = 0f;

            // 有効なクリップを適用
            for (int i = 0; i < clipCount; i++)
            {
                var inputPlayable = (ScriptPlayable<GlitchControlBehaviour>)playable.GetInput(i);
                float w = playable.GetInputWeight(i);
                if (w <= 0f) continue;

                totalWeight += w;
                var behaviour = inputPlayable.GetBehaviour();
                behaviour.Apply(proxy.Controller); // ← プロファイル/トグルをコントローラ経由で反映
            }

            // 1つもクリップが効いていないフレーム → リセット
            if (resetToZeroWhenNoClip && totalWeight <= 0.0001f)
                ResetToZero(proxy.Controller);
        }

        public override void OnGraphStop(Playable playable)
        {
            // タイムライン停止時の保険リセット
            if (resetToZeroWhenNoClip && _lastProxy != null && _lastProxy.Controller != null)
                ResetToZero(_lastProxy.Controller);
        }

        void ResetToZero(IGlitchController ctrl)
        {
            // 値を 0 に戻す（＝初期値0想定）
            ctrl.SetAnalogParams(0f, 0f, 0f, 0f);
            ctrl.SetDigitalIntensity(0f);

            // ついでにOFFにしたい場合
            if (disableWhenNoClip)
            {
                ctrl.SetAnalogEnabled(false);
                ctrl.SetDigitalEnabled(false);
            }
        }
    }
}
