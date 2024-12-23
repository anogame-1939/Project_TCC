using UnityEngine;
using System.Collections;
using System.Linq;

namespace AnoGame.Application.Enemy
{
    public class EnemyLifespan : MonoBehaviour
    {
        [SerializeField] private float minLifespan = 5f;
        [SerializeField] private float maxLifespan = 30f;
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private float particleFadeOutDuration = 0.5f;
        [SerializeField] private ParticleSystem disappearEffect;
        
        private SpriteRenderer[] _spriteRenderers;
        private ParticleSystem.MainModule _particleMainModule;
        public event System.Action OnLifespanExpired;
        
        
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
            
            StartCoroutine(DestroyAfterDelay());
        }
        
        private IEnumerator DestroyAfterDelay()
        {
            float delay = Random.Range(minLifespan, maxLifespan);
            yield return new WaitForSeconds(delay);

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
            yield return new WaitForSeconds(Mathf.Max(fadeOutDuration, particleFadeOutDuration));
            // パーティクルが完全に消えるまでの余裕を持たせる
            yield return new WaitForSeconds(0.1f);
            
            gameObject.SetActive(false);
        }
        
        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;
            Color[] initialColors = _spriteRenderers.Select(r => r.color).ToArray();
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / fadeOutDuration;
                
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
            
            while (elapsedTime < particleFadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / particleFadeOutDuration;
                
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