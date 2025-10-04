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
        [SerializeField] private float fadeIn = 0.2f;
        [SerializeField] private float stay = 2.0f;
        [SerializeField] private float fadeOut = 0.2f;
        [SerializeField]
        private UnityEvent _getEvent;

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

            // 表示
            gameObject.SetActive(true);
            _getEvent?.Invoke();
            await FadeTo(1f, fadeIn);
            await UniTask.Delay((int)(stay * 1000));
            await FadeTo(0f, fadeOut);

            // アンロード
            if (data.AssetReference != null) data.AssetReference.ReleaseAsset();
            gameObject.SetActive(false);
            _busy = false;
        }

        public bool IsBusy => _busy;

        public void MarkBusy() => _busy = true;

        private async UniTask FadeTo(float target, float duration)
        {
            group.gameObject.SetActive(true);
            group.alpha = 0f;
            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, target, t / duration);
                await UniTask.Yield();
            }
            group.alpha = target;
        }
    }
}
