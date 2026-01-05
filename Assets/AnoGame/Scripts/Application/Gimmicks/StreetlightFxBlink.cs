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

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float standardBlend = 0.6f; // Editorで変更可能にする

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

            UpdateVLB();
        }

        [Header("VLB Integration")]
        [SerializeField] private VLB.VolumetricLightBeamAbstractBase vlb;

        private void UpdateVLB()
        {
            if (vlb == null) return;

            // SD/HD specific update call
            if (vlb is VLB.VolumetricLightBeamSD sd)
            {
                sd.UpdateAfterManualPropertyChange();
            }
            else if (vlb is VLB.VolumetricLightBeamHD hd)
            {
                hd.UpdateAfterManualPropertyChange();
            }
        }

        /// <summary>
        /// マスター/論理 ON/OFF から「標準状態」の見た目へ即時反映
        /// ※ Controller から呼ばれる前提
        /// </summary>
        private void ApplyVisual()
        {
            // ロジック的に ON の時は standardBlend (デフォルト0.6)、OFF のときは 0
            float t = (_masterEnabled && _logicalOn) ? standardBlend : 0f;
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

        [Header("Blink Settings (Event Default)")]
        [SerializeField] private float defaultBlinkInterval = 0.1f;
        [SerializeField] private int defaultBlinkCount = 5;
        [SerializeField] private float defaultFadeDuration = 0.5f;

        /// <summary>
        /// Update設定されたデフォルト値で点滅消灯する（UnityEvent用）
        /// </summary>
        public void BlinkOff()
        {
            BlinkOff(defaultBlinkInterval, defaultBlinkCount, defaultFadeDuration);
        }

        /// <summary>
        /// 指定秒数（duration）点滅してから消灯する（UnityEvent用 helper）
        /// 点滅回数（count）は duration / defaultBlinkInterval から自動計算されます。
        /// </summary>
        /// <param name="duration">点滅し続ける時間（秒）</param>
        public void BlinkOffDuration(float duration)
        {
            if (defaultBlinkInterval <= 0.001f)
            {
                BlinkOff(defaultBlinkInterval, 1, defaultFadeDuration);
                return;
            }
            int count = Mathf.FloorToInt(duration / defaultBlinkInterval);
            if (count < 1) count = 1;
            BlinkOff(defaultBlinkInterval, count, defaultFadeDuration);
        }

        /// <summary>
        /// 指定回数点滅してから消灯する (Script用)
        /// blinkInterval: 点滅の1サイクル(ON->OFF)にかかる時間
        /// count: 点滅回数
        /// fadeDuration: 最後の消灯フェード時間
        /// </summary>
        public void BlinkOff(float blinkInterval, int count, float fadeDuration)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(BlinkOffRoutine(blinkInterval, count, fadeDuration));
            _logicalOn = false; // 最終的に消えるので論理OFF
        }

        private IEnumerator BlinkOffRoutine(float blinkInterval, int count, float fadeDuration)
        {
            // まずはパカパカさせる
            for (int i = 0; i < count; i++)
            {
                // OFF
                SetBlend(0f);
                yield return new WaitForSeconds(blinkInterval * 0.5f);
                // ON
                SetBlend(1f);
                yield return new WaitForSeconds(blinkInterval * 0.5f);
            }

            // 最後にフェードアウト
            yield return FadeRoutine(0f, fadeDuration);
        }

        /// <summary>
        /// Update設定されたデフォルト値で点滅点灯する（UnityEvent用）
        /// </summary>
        public void BlinkOn()
        {
            BlinkOn(defaultBlinkInterval, defaultBlinkCount, defaultFadeDuration);
        }

        /// <summary>
        /// 指定回数点滅してから点灯する (Script用)
        /// </summary>
        public void BlinkOn(float blinkInterval, int count, float fadeDuration)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(BlinkOnRoutine(blinkInterval, count, fadeDuration));
            _logicalOn = true; // 最終的に点くので論理ON
        }

        private IEnumerator BlinkOnRoutine(float blinkInterval, int count, float fadeDuration)
        {
            // パカパカ
            for (int i = 0; i < count; i++)
            {
                // OFF
                SetBlend(0f);
                yield return new WaitForSeconds(blinkInterval * 0.5f);
                // ON
                SetBlend(1f);
                yield return new WaitForSeconds(blinkInterval * 0.5f);
            }

            // 最後にフェードイン (1.0へ)
            // ※ ApplyVisual的には standardBlend になるべきかもしれないが、
            //    既存の FadeOn が 1.0f になっているのでそれに合わせる
            yield return FadeRoutine(1f, fadeDuration);
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
