using UnityEngine;
using System.Collections;
using System.Linq;

namespace AnoGame.Application.Enemy
{
    public class EnemyLifespan : MonoBehaviour
    {
        [SerializeField] private bool isPermanent = false;
        [SerializeField] private float minLifespan = 5f;
        [SerializeField] private float maxLifespan = 30f;
        [SerializeField] private bool _isAlive = false;
        [SerializeField] public bool IsAlive => _isAlive;
        [SerializeField] private float _fadeOutDuration = 1f;
        [SerializeField] private float _particleFadeOutDuration = 0.5f;
        [SerializeField] private ParticleSystem disappearEffect;
        [SerializeField] private ParticleSystem disappearEffect2;

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

            if (disappearEffect != null)
            {
                disappearEffect.gameObject.SetActive(false);
                _particleMainModule = disappearEffect.main;
            }
        }

        private void OnEnable()
        {
            ResetState();
            if (!isPermanent)
            {
                StartDestroyTimer();
            }
        }

        private void OnDisable()
        {
            StopDestroyTimer();
        }

        private void ResetState()
        {
            // 通常のアルファ値を1にリセット
            foreach (var renderer in _spriteRenderers)
            {
                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;

                // DissolveShaderのプロパティをリセット
                Material mat = renderer.material;
                if (mat.HasProperty("_DissolveAmount"))
                {
                    mat.SetFloat("_DissolveAmount", 0f); // 0f: 溶解なし
                }
                if (mat.HasProperty("_OutlineColor"))
                {
                    // 必要に応じてデフォルト値に戻す
                    mat.SetColor("_OutlineColor", Color.black);
                }
            }

            if (disappearEffect != null)
            {
                disappearEffect.gameObject.SetActive(false);
                disappearEffect.Stop();
                _particleMainModule.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            }
        }

        // 外部から寿命タイマーを開始
        public void StartDestroyTimer()
        {
            StopDestroyTimer();
            _destroyCoroutine = StartCoroutine(DestroyAfterDelay());
            _isAlive = true;
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

            _isAlive = false;
            StartCoroutine(FadeOutAndDestroy());
        }

        public void TriggerFadeOutAndDestroy()
        {
            StopDestroyTimer();
            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// 完全に消えるまでのフェードアウト + パーティクル再生
        /// </summary>
        private IEnumerator FadeOutAndDestroy()
        {
            // 寿命終了イベントを発火
            OnLifespanExpired?.Invoke();

            // 既存のアルファフェード処理
            StartCoroutine(FadeOut());

            // パーティクルエフェクトの再生
            if (disappearEffect != null)
            {
                disappearEffect.gameObject.SetActive(true);
                disappearEffect.Play();
                StartCoroutine(FadeOutParticle());
            }

            // フェードアウトとパーティクルのうち、長い方に合わせて待機
            yield return new WaitForSeconds(Mathf.Max(_fadeOutDuration, _particleFadeOutDuration));
            // パーティクルが完全に消えるまでの余裕を持たせる
            yield return new WaitForSeconds(0.1f);

            // 最終的に非アクティブ化
            gameObject.SetActive(false);
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

            // 部分フェードアウト開始時にパーティクルを再生
            if (disappearEffect2 != null)
            {
                disappearEffect2.gameObject.SetActive(true);
                disappearEffect2.Play();
            }

            // アウトラインカラーを一度だけ設定（必要ならコルーチン内で補間も可能）
            foreach (var renderer in _spriteRenderers)
            {
                Material mat = renderer.material;
                if (mat.HasProperty("_OutlineColor"))
                {
                    mat.SetColor("_OutlineColor", settings.outlineColor);
                }
            }

            // DissolveAmount のコルーチンを開始
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
                if (mat.HasProperty("_DissolveAmount"))
                {
                    initialDissolveValues[i] = mat.GetFloat("_DissolveAmount");
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
                    if (mat.HasProperty("_DissolveAmount"))
                    {
                        // 現在の値から targetDissolve までを補間
                        float current = Mathf.Lerp(initialDissolveValues[i], targetDissolve, normalizedTime);
                        mat.SetFloat("_DissolveAmount", current);
                    }
                }

                yield return null;
            }

            // 最終的に targetDissolve で固定
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Material mat = _spriteRenderers[i].material;
                if (mat.HasProperty("_DissolveAmount"))
                {
                    mat.SetFloat("_DissolveAmount", targetDissolve);
                }
            }
        }
    }
}
