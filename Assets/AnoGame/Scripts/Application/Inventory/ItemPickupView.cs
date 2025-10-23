// Presentation/Inventory/ItemPickupView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace AnoGame.Application.Inventory
{
    public class ItemPickupView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;

        [Header("Timing")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private float fadeIn = 0.2f;
        [SerializeField] private float stay = 2.0f;
        [SerializeField] private float fadeOut = 0.2f;
        [SerializeField] private UnityEvent _startEvent;
        [SerializeField] private UnityEvent _getEvent;
        [SerializeField] private UnityEvent _endEvent;

        private bool _busy;

        /// <summary>
        /// ItemData から UI を埋めて、フェード表示 → 自動で閉じます
        /// </summary>
        public async UniTask ShowAsync(AnoGame.Data.ItemData data, int quantity = 1)
        {
            if (data == null) return;

            // 文言
            nameText.text = quantity > 1 ? $"{data.ItemName} ×{quantity}" : data.ItemName;
            descText.text = data.Description ?? string.Empty;

            // アイコンロード（AssetReferenceSprite 推奨）
            if (data.AssetReference != null && data.AssetReference.RuntimeKeyIsValid())
            {
                var handle = data.AssetReference.LoadAssetAsync<Sprite>();
                iconImage.sprite = await handle.Task;
            }
            else
            {
                iconImage.sprite = null;
            }

            // イベント開始時にちょっと遅延
            _startEvent?.Invoke();
            await UniTask.Delay((int)(startDelay * 1000));

            // 表示
            gameObject.SetActive(true);
            _getEvent?.Invoke();
            await FadeTo(0, 1f, fadeIn);
            await UniTask.Delay((int)(stay * 1000));
            await FadeTo(1f, 0f, fadeOut);

            // アンロード
            if (data.AssetReference != null) data.AssetReference.ReleaseAsset();
            gameObject.SetActive(false);
            _busy = false;

            _endEvent?.Invoke();
        }

        public bool IsBusy => _busy;

        public void MarkBusy() => _busy = true;

        private async UniTask FadeTo(float start, float target, float duration)
        {
            group.gameObject.SetActive(true);

            float t = 0f;

            // UIはゲームの一時停止に影響されない方が自然なら unscaledDeltaTime を推奨
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;  // あるいは Time.deltaTime のままでもOK
                group.alpha = Mathf.Lerp(start, target, t / duration);
                await UniTask.Yield();
            }
            group.alpha = target;
        }
    }
}
