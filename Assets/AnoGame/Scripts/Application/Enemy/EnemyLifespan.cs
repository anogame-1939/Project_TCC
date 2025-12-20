using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// 敵の寿命管理を行うクラス。
    /// リゾルブ（溶解）等のビジュアル表現は SpriteResolveController に委譲する。
    /// </summary>
    public class EnemyLifespan : MonoBehaviour
    {
        [SerializeField] private float minLifespan = 5f;
        [SerializeField] private float maxLifespan = 30f;
        [SerializeField] private float _fadeOutDuration = 1f;

        private SpriteResolveController _resolveController;
        public event System.Action OnLifespanExpired;

        private Coroutine _destroyCoroutine;

        private void Awake()
        {
            _resolveController = GetComponent<SpriteResolveController>();
            if (_resolveController == null)
            {
                Debug.LogWarning($"[EnemyLifespan] SpriteResolveControllerが見つかりません: {gameObject.name}");
            }
        }

        public void Activate()
        {
            _resolveController?.Activate();
        }

        public void Deactivate()
        {
            _resolveController?.Deactivate();
        }

        #region Lifespan Logic

        public void StartDestroyTimer()
        {
            StopDestroyTimer();
            _destroyCoroutine = StartCoroutine(DestroyAfterDelay());
        }

        public void StopDestroyTimer()
        {
            if (_destroyCoroutine != null)
            {
                StopCoroutine(_destroyCoroutine);
                _destroyCoroutine = null;
            }
        }

        private IEnumerator DestroyAfterDelay()
        {
            float delay = Random.Range(minLifespan, maxLifespan);
            yield return new WaitForSeconds(delay);
            TriggerFadeOutAndDestroy();
        }

        public void TriggerFadeOutAndDestroy()
        {
            StopDestroyTimer();
            FadeOutAndDestroyTask().Forget();
        }

        private async UniTaskVoid FadeOutAndDestroyTask()
        {
            OnLifespanExpired?.Invoke();

            if (_resolveController != null)
            {
                // デフォルトのフェードアウト設定（簡易的）
                var settings = ScriptableObject.CreateInstance<PartialFadeSettings>();
                settings.targetAlpha = 0f;
                settings.duration = _fadeOutDuration;
                settings.isNormal = true;

                await _resolveController.PlayFadeOutAsync(settings);
            }

            gameObject.SetActive(false);
        }

        #endregion

        #region Bridge Methods (for Backward Compatibility)

        public void StartFadeOut(float duration)
        {
            _fadeOutDuration = duration;
            TriggerFadeOutAndDestroy();
        }

        public void StartFadeOut()
        {
            TriggerFadeOutAndDestroy();
        }

        public void ImmediateDeactive()
        {
            gameObject.SetActive(false);
        }

        public void FadeToPartialState(PartialFadeSettings settings)
        {
            StopDestroyTimer();
            _resolveController?.TriggerFadeOut(settings);
        }

        public void CompletePartialFadeOut(float duration)
        {
            var settings = ScriptableObject.CreateInstance<PartialFadeSettings>();
            settings.targetAlpha = 0f;
            settings.duration = duration;
            settings.isNormal = true;
            FadeToPartialState(settings);
        }

        #endregion
    }
}
