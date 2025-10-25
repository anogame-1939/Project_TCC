// Presentation/Debug/DebugInventoryRow.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnoGame.Data;
using AnoGame.Application.Inventory; // ★ InventoryManager
// using AnoGame.Domain.Inventory.Services; // ←使わない

namespace AnoGame.SLFBDebug
{
    [AddComponentMenu("AnoGame/Debug/DebugInventoryRow")]
    public class DebugInventoryRow : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private TMP_Text label;       // Textを使うならTMP_Text→Textに変更可
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button consumeButton;

        [Header("Behavior")]
        [SerializeField, Min(1)] private int consumeQuantity = 1;

        // 内部保持
        private InventoryManager _inventoryManager; // ★ ここをInventoryManagerに
        private ItemData _item;
        private bool _showIdInLabel;

        /// <summary>
        /// UIと挙動を初期化
        /// </summary>
        public void Initialize(InventoryManager inventoryManager, ItemData item, int quantity = 1, bool showIdInLabel = false)
        {
            _inventoryManager = inventoryManager;
            _item = item;
            consumeQuantity = Mathf.Max(1, quantity);
            _showIdInLabel = showIdInLabel;

            // ラベル
            if (label != null)
            {
                label.text = _showIdInLabel
                    ? $"{_item.ItemName}  (ID:{_item.ItemId})"
                    : _item.ItemName;
            }

            // ボタン
            if (addButton != null)
            {
                addButton.onClick.RemoveAllListeners();
                addButton.onClick.AddListener(OnAddClicked);
            }
            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(OnRemoveClicked);
            }
            if (consumeButton != null)
            {
                consumeButton.onClick.RemoveAllListeners();
                consumeButton.onClick.AddListener(OnConsumeClicked);
            }
        }

        private void OnAddClicked()
        {
            if (_inventoryManager == null || _item == null) return;
            var ok = _inventoryManager.AddItem(_item, 1);
            Debug.Log($"[DebugInventoryRow] Add: {_item.ItemName} (ok={ok})");
        }

        private void OnRemoveClicked()
        {
            if (_inventoryManager == null || _item == null) return;
            var ok = _inventoryManager.RemoveItem(_item.ItemName, 1);
            Debug.Log($"[DebugInventoryRow] Remove: {_item.ItemName} (ok={ok})");
        }

        private void OnConsumeClicked()
        {
            if (_inventoryManager == null || _item == null) return;
            // 最小実装：消費 = インベントリから数量を減らす
            var ok = _inventoryManager.RemoveItem(_item.ItemName, consumeQuantity);
            Debug.Log($"[DebugInventoryRow] Consume(Remove): {_item.ItemName} x{consumeQuantity} (ok={ok})");

            // もし「消費イベント（OnItemConsumed）」も必要なら、
            // IInventoryService を別途注入してここで呼ぶブリッジを追加してください。
            // 例) _inventoryService.ConsumeItem(_item.ItemId, consumeQuantity, gameObject, transform.position);
        }
    }
}
