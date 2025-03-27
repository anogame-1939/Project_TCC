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
            foreach (var renderer in _spriteRenderers)
            {
                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;
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

        public void StartFadeOut(float duration)
        {
            Debug.Log("StartFadeOut");
            _particleFadeOutDuration = duration;
            _fadeOutDuration = duration;
            StartCoroutine(FadeOutAndDestroy());
        }

        private IEnumerator FadeOutCorourine(float duration)
        {
            yield return new WaitForSeconds(duration);

        }

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


        private IEnumerator FadeOutAndDestroy()
        {
            // 寿命終了イベントを発火
            OnLifespanExpired?.Invoke();

            // 既存のフェードアウト処理
            StartCoroutine(FadeOut());
            
            if (disappearEffect != null)
            {
                disappearEffect.gameObject.SetActive(true);
                disappearEffect.Play();
                StartCoroutine(FadeOutParticle());
            }

            // 両方のフェードアウトの長い方の時間だけ待機
            yield return new WaitForSeconds(Mathf.Max(_fadeOutDuration, _particleFadeOutDuration));
            // パーティクルが完全に消えるまでの余裕を持たせる
            yield return new WaitForSeconds(0.1f);
            
            gameObject.SetActive(false);
        }
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
            
            foreach (var renderer in _spriteRenderers)
            {
                Color color = renderer.color;
                color.a = 0f;
                renderer.color = color;
            }
        }

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
    }
}