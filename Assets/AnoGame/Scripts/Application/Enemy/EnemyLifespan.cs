using UnityEngine;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks.Triggers;
using Unity.VisualScripting;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Enemy
{
    public class EnemyLifespan : MonoBehaviour
    {
        [SerializeField] private float minLifespan = 5f;
        [SerializeField] private float maxLifespan = 30f;
        [SerializeField] private float _fadeOutDuration = 1f;
        [SerializeField] private float _particleFadeOutDuration = 0.5f;
        [SerializeField] private ParticleSystem fadeInEffect;
        [SerializeField] private ParticleSystem fadeoutEffect;
        [SerializeField] private ParticleSystem disappearEffect;
        [SerializeField] private GameObject[] shadowObjects;
        [SerializeField] private AnimationCurve shadowToBig = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve shadowToSmall = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float shadowScaleDuration = 0.35f;

        private const string DissolveAmountProperty = "_DissolveAmount";
        private const string OutlineColorProperty = "_OutlineColor";
        private SpriteRenderer[] _spriteRenderers;
        private ParticleSystem.MainModule _particleMainModule;
        public event System.Action OnLifespanExpired;

        private Coroutine _destroyCoroutine;

        private void Awake()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            if (_spriteRenderers.Length == 0)
            {
                Debug.LogWarning($"SpriteRendererが見つかりません: {gameObject.name}");
            }

            _particleMainModule = fadeoutEffect.main;
            // _particleMainModule = fadeoutEffect.main;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // 初期化処理
            fadeInEffect.Stop();
            fadeoutEffect.Stop();
            disappearEffect.Stop();

            Deactivate();
        }

        public void Activate()
        {
            // 表示状態とする
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    // 現在の値から targetDissolve までを補間
                    mat.SetFloat(DissolveAmountProperty, 0f);
                }
            }

            ShowShadow();
            StopAllParticle();
        }

        public void Deactivate()
        {
            // 非表示状態とする
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    // 現在の値から targetDissolve までを補間
                    mat.SetFloat(DissolveAmountProperty, 1f);
                }
            }

            HideShadow();
        }

        private void ShowShadow()
        {
            for (int i = 0; i < shadowObjects.Length; i++)
            {
                shadowObjects[i].transform.localScale = Vector3.one;
            }
        }

        private void HideShadow()
        {
            for (int i = 0; i < shadowObjects.Length; i++)
            {
                shadowObjects[i].transform.localScale = Vector3.zero;
            }
        }

        

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
            // StopDestroyTimer();
        }

        private void ResetState()
        {
            SwitchSpiriteRenderers(true);

            // 通常のアルファ値を1にリセット
            foreach (var renderer in _spriteRenderers)
            {
                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;

                // DissolveShaderのプロパティをリセット
                Material mat = renderer.material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    mat.SetFloat(DissolveAmountProperty, 1f);
                }
            }

            if (fadeoutEffect != null)
            {
                fadeoutEffect.gameObject.SetActive(false);
                fadeoutEffect.Stop();
                _particleMainModule.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            }
        }

        // 外部から寿命タイマーを開始
        public void StartDestroyTimer()
        {
            StopDestroyTimer();
            _destroyCoroutine = StartCoroutine(DestroyAfterDelay());
        }

        // 外部から寿命タイマーを停止
        public void StopDestroyTimer()
        {
            if (_destroyCoroutine != null)
            {
                StopCoroutine(_destroyCoroutine);
                _destroyCoroutine = null;
            }
        }

        // 完全なフェードアウトを開始(時間指定)
        public void StartFadeOut(float duration)
        {
            Debug.Log("StartFadeOut");
            _particleFadeOutDuration = duration;
            _fadeOutDuration = duration;
            StartCoroutine(FadeOutAndDestroy());
        }

        // 完全なフェードアウトを開始(デフォルト時間)
        public void StartFadeOut()
        {
            StopDestroyTimer();
            StartCoroutine(FadeOutAndDestroy());
        }

        public void ImmediateDeactive()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator DestroyAfterDelay()
        {
            float delay = Random.Range(minLifespan, maxLifespan);
            yield return new WaitForSeconds(delay);

            StartCoroutine(FadeOutAndDestroy());
        }

        public void TriggerFadeOutAndDestroy()
        {
            StopDestroyTimer();
            StartCoroutine(FadeOutAndDestroy());
        }


        // TODO:消すかも
        /// <summary>
        /// 完全に消えるまでのフェードアウト + パーティクル再生
        /// </summary>
        private IEnumerator FadeOutAndDestroy()
        {
            Debug.Log("使ってる？======================================================================================");
            // 寿命終了イベントを発火
            OnLifespanExpired?.Invoke();

            // 既存のアルファフェード処理
            StartCoroutine(FadeOut());

            // パーティクルエフェクトの再生
            if (fadeoutEffect != null)
            {
                fadeoutEffect.gameObject.SetActive(true);
                fadeoutEffect.Play();
                StartCoroutine(FadeOutParticle());
            }

            // フェードアウトとパーティクルのうち、長い方に合わせて待機
            yield return new WaitForSeconds(Mathf.Max(_fadeOutDuration, _particleFadeOutDuration));
            // パーティクルが完全に消えるまでの余裕を持たせる
            yield return new WaitForSeconds(0.1f);

            fadeoutEffect.Stop();

            yield return new WaitForSeconds(1f);

            SwitchSpiriteRenderers(false);

            // 最終的に非アクティブ化
            gameObject.SetActive(false);
        }

        private void SwitchSpiriteRenderers(bool isActive)
        {
            foreach (var spriteRenderer in _spriteRenderers)
            {
                spriteRenderer.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// アルファ値を使った既存フェードアウト処理
        /// </summary>
        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;
            Color[] initialColors = _spriteRenderers.Select(r => r.color).ToArray();

            while (elapsedTime < _fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _fadeOutDuration;

                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    Color color = initialColors[i];
                    color.a = Mathf.Lerp(1f, 0f, normalizedTime);
                    _spriteRenderers[i].color = color;
                }

                yield return null;
            }

            // 最終的にアルファを0に
            foreach (var renderer in _spriteRenderers)
            {
                Color color = renderer.color;
                color.a = 0f;
                renderer.color = color;
            }
        }

        /// <summary>
        /// パーティクルのフェードアウト処理
        /// </summary>
        private IEnumerator FadeOutParticle()
        {
            float elapsedTime = 0f;
            Color startColor = _particleMainModule.startColor.color;

            while (elapsedTime < _particleFadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _particleFadeOutDuration;

                Color newColor = startColor;
                newColor.a = Mathf.Lerp(1f, 0f, normalizedTime);
                _particleMainModule.startColor = new ParticleSystem.MinMaxGradient(newColor);

                yield return null;
            }

            Color finalColor = startColor;
            finalColor.a = 0f;
            _particleMainModule.startColor = new ParticleSystem.MinMaxGradient(finalColor);
        }

        /// <summary>
        /// 部分的なフェードアウト(溶解表現)を行うメソッド
        /// DissolveAmount + OutlineColor を設定
        /// </summary>
        public void FadeToPartialState(PartialFadeSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("PartialFadeSettingsがnullです。");
                return;
            }

            // 既存の破壊タイマーを停止
            StopDestroyTimer();

            if (settings.isNormal)
            {
                // 部分フェードアウト開始時にパーティクルを再生
                if (fadeoutEffect != null)
                {
                    fadeoutEffect.gameObject.SetActive(true);
                    fadeoutEffect.Play();
                }
            }
            else
            {
                // 部分フェードアウト開始時にパーティクルを再生
                if (disappearEffect != null)
                {
                    disappearEffect.gameObject.SetActive(true);
                    disappearEffect.Play();
                }
            }



            // アウトラインカラーを一度だけ設定（必要ならコルーチン内で補間も可能）
            foreach (var renderer in _spriteRenderers)
            {
                Material mat = renderer.material;
                if (mat.HasProperty(OutlineColorProperty))
                {
                    mat.SetColor(OutlineColorProperty, settings.outlineColor);
                }
            }

            // DissolveAmount のコルーチンを開始（部分的なフェードアウト）
            StartCoroutine(FadeOutPartialCoroutine(settings.targetAlpha, settings.duration));
        }

        /// <summary>
        /// DissolveAmount を補間してキャラクターを溶解状態にするコルーチン
        /// </summary>
        private IEnumerator FadeOutPartialCoroutine(float targetDissolve, float duration)
        {
            float elapsedTime = 0f;

            // SpriteRenderer ごとに現在の DissolveAmount を取得
            float[] initialDissolveValues = new float[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    initialDissolveValues[i] = mat.GetFloat(DissolveAmountProperty);
                }
                else
                {
                    // シェーダーに _DissolveAmount が無い場合は 0 として扱う
                    initialDissolveValues[i] = 0f;
                }
            }

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;

                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    Material mat = _spriteRenderers[i].material;
                    if (mat.HasProperty(DissolveAmountProperty))
                    {
                        // 現在の値から targetDissolve までを補間
                        float current = Mathf.Lerp(initialDissolveValues[i], targetDissolve, normalizedTime);
                        mat.SetFloat(DissolveAmountProperty, current);
                    }
                }

                yield return null;
            }

            // 最終的に targetDissolve で固定
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    mat.SetFloat(DissolveAmountProperty, targetDissolve);
                }
            }
        }

        public IEnumerator PlayFadInParticle(PartialFadeSettings settings)
        {
            if (settings == null) yield break;
            fadeInEffect.Play();

            foreach (var sr in _spriteRenderers)
            {
                var mat = sr.material;
                if (mat.HasProperty(OutlineColorProperty))
                    mat.SetColor(OutlineColorProperty, settings.outlineColor);

                if (mat.HasProperty(DissolveAmountProperty))
                    mat.SetFloat(DissolveAmountProperty, 1f);
            }

            // --- ② 開始／終了値を算出 ---
            float[] startVals = new float[_spriteRenderers.Length];
            float[] endVals   = new float[_spriteRenderers.Length];

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                var mat = _spriteRenderers[i].material;
                startVals[i] = mat.HasProperty(DissolveAmountProperty)
                            ? mat.GetFloat(DissolveAmountProperty) : 0f;
                endVals[i]   = 1 - settings.targetAlpha;
            }

            // --- ③ フェード本体と影スケールを並列起動 ---
            var dissolveCoroutine = StartCoroutine(DissolveCoroutine(startVals, endVals, settings.duration));
            var shadowCoroutine   = StartCoroutine(ShadowScaleCoroutine());

            // --- ④ 2 本とも終わるまで順に待機 ---
            yield return dissolveCoroutine;
            yield return shadowCoroutine;
        }

        public IEnumerator PlayFadOutParticle(PartialFadeSettings settings)
        {
            if (settings == null) yield break;
            fadeoutEffect.Play();

            foreach (var sr in _spriteRenderers)
            {
                var mat = sr.material;
                if (mat.HasProperty(OutlineColorProperty))
                    mat.SetColor(OutlineColorProperty, settings.outlineColor);

                if (mat.HasProperty(DissolveAmountProperty))
                    mat.SetFloat(DissolveAmountProperty, 1f);
            }

            // --- ② 開始／終了値を算出 ---
            float[] startVals = new float[_spriteRenderers.Length];
            float[] endVals   = new float[_spriteRenderers.Length];

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                var mat = _spriteRenderers[i].material;
                startVals[i] = mat.HasProperty(DissolveAmountProperty)
                            ? mat.GetFloat(DissolveAmountProperty) : 0f;
                endVals[i]   = settings.targetAlpha;
            }

            // --- ③ フェード本体と影スケールを並列起動 ---
            var dissolveCoroutine = StartCoroutine(DissolveCoroutine(startVals, endVals, settings.duration));
            var shadowCoroutine   = StartCoroutine(ShadowScaleCoroutine(true));

            // --- ④ 2 本とも終わるまで順に待機 ---
            yield return dissolveCoroutine;
            yield return shadowCoroutine;
        }

        /// <summary>
        /// SpriteRenderer 群の DissolveAmount を duration 秒かけて補間する
        /// </summary>
        private IEnumerator DissolveCoroutine(float[] startVals, float[] endVals, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    float val = Mathf.Lerp(startVals[i], endVals[i], t);
                    var mat = _spriteRenderers[i].material;
                    if (mat.HasProperty(DissolveAmountProperty))
                        mat.SetFloat(DissolveAmountProperty, val);
                }
                yield return null;
            }

            // 最終値を固定
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                var mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                    mat.SetFloat(DissolveAmountProperty, endVals[i]);
            }
        }

        /// <summary>
        /// 影オブジェクトを shadowScaleDuration 秒でスケール1にする
        /// </summary>
        private IEnumerator ShadowScaleCoroutine(bool toSmall = false)
        {
            float elapsed = 0f;
            float[] start = shadowObjects.Select(s => s.transform.localScale.x).ToArray();

            while (elapsed < shadowScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shadowScaleDuration);

                // ── カーブで加速／減速を制御
                float curveT = toSmall? shadowToSmall.Evaluate(t) : shadowToBig.Evaluate(t);  // 0→1 の非線形補間係数

                for (int i = 0; i < shadowObjects.Length; i++)
                {
                    float scale = toSmall ? Mathf.Lerp(start[i], 0f, curveT) : Mathf.Lerp(start[i], 1f, curveT);
                    // Debug.Log($"invers:{toSmall}...sclae:{scale}");
                    shadowObjects[i].transform.localScale = Vector3.one * scale;
                }
                yield return null;
            }

            // 終了時に完全な 1 を保証
            // foreach (var sh in shadowObjects)
                // sh.transform.localScale = Vector3.one;
        }

        private void StopAllParticle()
        {
            fadeInEffect.Stop();
            fadeoutEffect.Stop();
            disappearEffect.Stop();
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public async UniTask PlayFadeInAsync(PartialFadeSettings settings)
        {
            if (settings == null) return;
            fadeInEffect.Play();

            // マテリアル初期値セット
            foreach (var sr in _spriteRenderers)
            {
                var mat = sr.material;
                if (mat.HasProperty(OutlineColorProperty))
                    mat.SetColor(OutlineColorProperty, settings.outlineColor);
                if (mat.HasProperty(DissolveAmountProperty))
                    mat.SetFloat(DissolveAmountProperty, 1f);
            }

            // 開始／終了値
            var startVals = _spriteRenderers
                .Select(sr => sr.material.HasProperty(DissolveAmountProperty)
                            ? sr.material.GetFloat(DissolveAmountProperty)
                            : 0f)
                .ToArray();
            var endVals = Enumerable.Repeat(1f - settings.targetAlpha, _spriteRenderers.Length).ToArray();

            // 並列実行して待機
            var dissolveTask = DissolveAsync(startVals, endVals, settings.duration);
            var shadowTask = ShadowScaleAsync(false);
            await UniTask.WhenAll(dissolveTask, shadowTask);
        }

        /// <summary>
        /// 溶かしながらフェードアウト ⇒ UniTask版
        /// </summary>
        public async UniTask PlayFadeOutAsync(PartialFadeSettings settings)
        {
            if (settings == null) return;
            fadeoutEffect.Play();

            foreach (var sr in _spriteRenderers)
            {
                var mat = sr.material;
                if (mat.HasProperty(OutlineColorProperty))
                    mat.SetColor(OutlineColorProperty, settings.outlineColor);
                // if (mat.HasProperty(DissolveAmountProperty))
                //     mat.SetFloat(DissolveAmountProperty, 1f);
            }

            var startVals = _spriteRenderers
                .Select(sr => sr.material.HasProperty(DissolveAmountProperty)
                            ? sr.material.GetFloat(DissolveAmountProperty)
                            : 0f)
                .ToArray();
            var endVals = Enumerable.Repeat(1 - settings.targetAlpha, _spriteRenderers.Length).ToArray();

            var dissolveTask = DissolveAsync(startVals, endVals, settings.duration);
            var shadowTask   = ShadowScaleAsync(true);
            await UniTask.WhenAll(dissolveTask, shadowTask);
        }

        /// <summary>
        /// SpriteRenderer群のDissolveAmountをduration秒かけて補間するUniTask版
        /// </summary>
        private async UniTask DissolveAsync(float[] startVals, float[] endVals, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    float val = Mathf.Lerp(startVals[i], endVals[i], t);
                    Debug.Log($"val:{val}");
                    var mat = _spriteRenderers[i].material;
                    if (mat.HasProperty(DissolveAmountProperty))
                        mat.SetFloat(DissolveAmountProperty, val);
                }

                // 次フレームまで待つ
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 最終値を確定
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                var mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                    mat.SetFloat(DissolveAmountProperty, endVals[i]);
            }
        }

        /// <summary>
        /// 影のスケールを UniTask で補間する
        /// </summary>
        private async UniTask ShadowScaleAsync(bool toSmall = false)
        {
            float elapsed = 0f;
            // 現在のスケール値（x 成分）を取得
            float[] startScales = shadowObjects
                .Select(s => s.transform.localScale.x)
                .ToArray();

            while (elapsed < shadowScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shadowScaleDuration);

                // カーブで加速／減速を制御
                float curveT = toSmall
                    ? shadowToSmall.Evaluate(t)  // 0→1 の非線形補間係数
                    : shadowToBig.Evaluate(t);

                for (int i = 0; i < shadowObjects.Length; i++)
                {
                    float target = toSmall
                        ? Mathf.Lerp(startScales[i], 0f, curveT)
                        : Mathf.Lerp(startScales[i], 1f, curveT);

                    shadowObjects[i].transform.localScale = Vector3.one * target;
                }

                // 次フレームまで待機
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        /// <summary>
        /// 部分的なフェードアウト状態から残りを完全にフェードアウトさせる処理
        /// 現在の _DissolveAmount の値から 1（完全溶解）になるまで補間します。
        /// </summary>
        public void CompletePartialFadeOut(float duration)
        {
            StopDestroyTimer();
            StartCoroutine(CompletePartialFadeOutCoroutine(duration));
        }

        private IEnumerator CompletePartialFadeOutCoroutine(float duration)
        {
            // 現在の DissolveAmount の値を各レンダラーから取得
            float[] currentDissolveValues = new float[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    currentDissolveValues[i] = mat.GetFloat(DissolveAmountProperty);
                }
                else
                {
                    currentDissolveValues[i] = 0f;
                }
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    Material mat = _spriteRenderers[i].material;
                    if (mat.HasProperty(DissolveAmountProperty))
                    {
                        // 現在の値から 1（完全に溶解）までを補間
                        float newVal = Mathf.Lerp(currentDissolveValues[i], 1f, normalizedTime);
                        mat.SetFloat(DissolveAmountProperty, newVal);
                    }
                }
                yield return null;
            }

            // 最終的に各レンダラーの DissolveAmount を 1 に固定
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty(DissolveAmountProperty))
                {
                    mat.SetFloat(DissolveAmountProperty, 1f);
                }
            }

            // 部分フェードアウト開始時にパーティクルを再生
            if (fadeoutEffect != null)
            {
                fadeoutEffect.Stop();
            }

            // 部分フェードアウト開始時にパーティクルを再生
            if (disappearEffect != null)
            {
                disappearEffect.Stop();
            }
        }
    }
}
