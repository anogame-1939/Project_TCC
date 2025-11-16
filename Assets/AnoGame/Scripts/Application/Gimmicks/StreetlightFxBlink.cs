using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Gimmicks
{
    public class StreetlightFxBlink : MonoBehaviour
    {
        [SerializeField]
        private string bindingId;   // 例: "Streetlight_A", "GateLamp_01" など
        public string BindingId => bindingId;

        [Header("Targets")]
        [SerializeField] private Light spotLight;
        [SerializeField] private Renderer bulbRenderer; // 電球メッシュ

        [Header("Emission")]
        [SerializeField] private Color emissionOn = new Color(1f, 0.85f, 0.6f, 1f);
        [SerializeField] private Color emissionOff = new Color(0.05f, 0.05f, 0.05f, 1f);
        [SerializeField] private float emissionIntensity = 2.0f; // 乗算

        [Header("Spot")]
        [SerializeField] private float spotOnIntensity = 1.2f;
        [SerializeField] private float spotOffIntensity = 0.0f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;
        private bool _masterEnabled = true;
        private bool _logicalOn = true; // Timeline 等からの論理 ON/OFF

        // フェード制御用
        private float _currentBlend = 1f;     // 直近の SetBlend 値
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ApplyVisual();
        }

        public void SetMasterEnabled(bool enabled)
        {
            _masterEnabled = enabled;
            ApplyVisual();
        }

        public void SetLogicalOn(bool on)
        {
            _logicalOn = on;
            ApplyVisual();
        }

        /// <summary>
        /// 0〜1 でブレンド（即時反映）
        /// Timeline / DOTween / コードから直接叩く低レベル API
        /// </summary>
        public void SetBlend(float t)
        {
            t = Mathf.Clamp01(t);
            _currentBlend = t;

            bool effectiveOn = _masterEnabled && (t > 0.001f);

            if (spotLight != null)
            {
                spotLight.enabled = effectiveOn;
                spotLight.intensity = Mathf.Lerp(spotOffIntensity, spotOnIntensity, t);
            }

            if (bulbRenderer != null)
            {
                bulbRenderer.GetPropertyBlock(_mpb);
                var col = Color.Lerp(emissionOff, emissionOn, t) * emissionIntensity;
                _mpb.SetColor(EmissionColorId, col);
                bulbRenderer.SetPropertyBlock(_mpb);

                var mat = bulbRenderer.sharedMaterial;
                if (mat != null) mat.EnableKeyword("_EMISSION");
            }
        }

        /// <summary>
        /// マスター/論理 ON/OFF から「標準状態」の見た目へ即時反映
        /// ※ Controller から呼ばれる前提
        /// </summary>
        private void ApplyVisual()
        {
            // ロジック的に ON の時はちょい暗め (0.6)、OFF のときは 0
            float t = (_masterEnabled && _logicalOn) ? 0.6f : 0f;
            SetBlend(t);
        }

        [Button]
        public void Toggle()
        {
            SetLogicalOn(!_logicalOn);
        }

        #region Fade API

        /// <summary>
        /// 現在の明るさから「完全 ON (1)」まで duration 秒かけてフェード
        /// </summary>
        public void FadeOn(float duration)
        {
            StartFade(1f, duration);
            _logicalOn = true; // 論理状態も ON 側へ寄せておく（Controller との整合のため）
        }

        /// <summary>
        /// 現在の明るさから「完全 OFF (0)」まで duration 秒かけてフェード
        /// </summary>
        public void FadeOff(float duration)
        {
            StartFade(0f, duration);
            _logicalOn = false;
        }

        private void StartFade(float target, float duration)
        {
            if (duration <= 0f)
            {
                // 即時反映
                SetBlend(target);
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(target, duration));
        }

        private IEnumerator FadeRoutine(float target, float duration)
        {
            float start = _currentBlend;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float blend = Mathf.Lerp(start, target, t);

                SetBlend(blend);
                yield return null;
            }

            SetBlend(target);
            _fadeRoutine = null;
        }

        #endregion
    }
}
