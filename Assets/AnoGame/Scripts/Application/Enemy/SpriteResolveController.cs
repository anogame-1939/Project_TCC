using UnityEngine;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// スプライトの溶解（リゾルブ）処理に特化したコンポーネント。
    /// ゲームロジックとは切り離され、純粋なビジュアル制御を行う。
    /// </summary>
    public class SpriteResolveController : MonoBehaviour
    {
        [SerializeField] private float _defaultDuration = 1f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem disappearEffect;

        [Header("Thresholds (0-1)")]
        [SerializeField, Range(0f, 1f)] private float playThreshold = 0.2f;
        [SerializeField, Range(0f, 1f)] private float stopThreshold = 0.8f;

        [Header("Shadows")]
        [SerializeField] private GameObject[] shadowObjects;
        [SerializeField] private AnimationCurve shadowToBig = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve shadowToSmall = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float shadowScaleDuration = 0.35f;

        private const string DissolveAmountProperty = "_DissolveAmount";
        private const string OutlineColorProperty = "_OutlineColor";

        private SpriteRenderer[] _spriteRenderers;
        private bool _isEffectPlaying = false;

        private void Awake()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (_spriteRenderers.Length == 0)
            {
                Debug.LogWarning($"[SpriteResolveController] SpriteRendererが見つかりません: {gameObject.name}");
            }
        }

        #region UnityEvent / Public API

        /// <summary>
        /// リゾルブ値を直接設定（0: 通常表示, 1: 完全に溶けた状態）
        /// </summary>
        public void SetResolve(float amount)
        {
            foreach (var sr in _spriteRenderers)
            {
                Material mat = sr.material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    mat.SetFloat(DissolveAmountProperty, amount);
                }
            }
        }

        public void SetOutlineColor(Color color)
        {
            foreach (var sr in _spriteRenderers)
            {
                Material mat = sr.material;
                if (mat.HasProperty(OutlineColorProperty))
                {
                    mat.SetColor(OutlineColorProperty, color);
                }
            }
        }

        public void Activate()
        {
            SetResolve(0f);
            ShowShadow();
            StopAllEffects();
        }

        public void Deactivate()
        {
            SetResolve(1f);
            HideShadow();
        }

        public async UniTask PlayFadeInAsync(PartialFadeSettings settings)
        {
            if (settings == null) return;
            await ExecuteFadeAsync(settings, true);
        }

        public async UniTask PlayFadeOutAsync(PartialFadeSettings settings)
        {
            if (settings == null) return;
            await ExecuteFadeAsync(settings, false);
        }

        // UnityEventから呼び出し可能なラップメソッド
        public void TriggerFadeIn(PartialFadeSettings settings) => PlayFadeInAsync(settings).Forget();
        public void TriggerFadeOut(PartialFadeSettings settings) => PlayFadeOutAsync(settings).Forget();

        /// <summary>
        /// タイムライン等から毎フレーム進捗を受け取り、エフェクトの再生・停止を制御する。
        /// </summary>
        public void HandleEffect(float progress, bool playEffect, float customPlayThreshold = -1f, float customStopThreshold = -1f)
        {
            if (!playEffect || disappearEffect == null)
            {
                if (_isEffectPlaying)
                {
                    disappearEffect?.Stop();
                    _isEffectPlaying = false;
                }
                return;
            }

            float pThresh = customPlayThreshold >= 0 ? customPlayThreshold : playThreshold;
            float sThresh = customStopThreshold >= 0 ? customStopThreshold : stopThreshold;

            if (!_isEffectPlaying && progress >= pThresh && progress < sThresh)
            {
                disappearEffect.Play();
                _isEffectPlaying = true;
            }
            else if (_isEffectPlaying && (progress >= sThresh || progress < pThresh))
            {
                disappearEffect.Stop();
                _isEffectPlaying = false;
            }
        }

        #endregion

        #region Internal Logic

        private async UniTask ExecuteFadeAsync(PartialFadeSettings settings, bool isIn)
        {
            SetOutlineColor(settings.outlineColor);

            float duration = settings.duration > 0 ? settings.duration : _defaultDuration;
            float targetResolve = isIn ? (1f - settings.targetAlpha) : settings.targetAlpha;
            float startResolve = GetCurrentResolve();

            float elapsed = 0f;

            // 影と並列実行
            var shadowTask = ShadowScaleAsync(duration, !isIn);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentVal = Mathf.Lerp(startResolve, targetResolve, t);
                SetResolve(currentVal);

                // エフェクト制御
                HandleEffect(t, true);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            SetResolve(targetResolve);
            StopAllEffects();
            await shadowTask;
        }

        private float GetCurrentResolve()
        {
            if (_spriteRenderers == null || _spriteRenderers.Length == 0) return 0f;
            var mat = _spriteRenderers[0].material;
            return mat.HasProperty(DissolveAmountProperty) ? mat.GetFloat(DissolveAmountProperty) : 0f;
        }

        private async UniTask ShadowScaleAsync(float duration, bool toSmall)
        {
            if (shadowObjects == null || shadowObjects.Length == 0) return;

            float elapsed = 0f;
            float[] startScales = shadowObjects.Select(s => s.transform.localScale.x).ToArray();

            while (elapsed < shadowScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shadowScaleDuration);
                float curveT = toSmall ? shadowToSmall.Evaluate(t) : shadowToBig.Evaluate(t);

                for (int i = 0; i < shadowObjects.Length; i++)
                {
                    float scale = toSmall ? Mathf.Lerp(startScales[i], 0f, curveT) : Mathf.Lerp(startScales[i], 1f, curveT);
                    shadowObjects[i].transform.localScale = Vector3.one * scale;
                }
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            float finalScale = toSmall ? 0f : 1f;
            foreach (var sh in shadowObjects) sh.transform.localScale = Vector3.one * finalScale;
        }

        public void ShowShadow()
        {
            if (shadowObjects == null) return;
            foreach (var sh in shadowObjects) sh.transform.localScale = Vector3.one;
        }

        public void HideShadow()
        {
            if (shadowObjects == null) return;
            foreach (var sh in shadowObjects) sh.transform.localScale = Vector3.zero;
        }

        public void StopAllEffects()
        {
            if (disappearEffect != null)
            {
                disappearEffect.Stop();
                _isEffectPlaying = false;
            }
        }

        #endregion
    }
}
