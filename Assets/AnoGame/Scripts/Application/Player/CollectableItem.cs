using VContainer;
using UnityEngine;
using AnoGame.Data;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame
{
    [AddComponentMenu("Inventory/" + nameof(CollectableItem))]
    public class CollectableItem : MonoBehaviour
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;
        [SerializeField] private string uniqueId;
        private ItemData _previousItemData;

        public ItemData ItemData => itemData;
        public int Quantity => quantity;
        public string UniqueId => uniqueId;

        [Inject] private IEventService _eventService;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;

            if (itemData.IsStackable)
            {
                // スタッカブルアイテムはユニークIDで登録
                _eventService.RegisterKeyItemHandler(uniqueId, DisableItem);
            }
            else
            {
                // 非スタッカブルアイテムはアイテム名で登録
                _eventService.RegisterKeyItemHandler(itemData.ItemName, DisableItem);
            }
        }

        private void OnDestroy()
        {
            if (itemData != null)
            {
                if (itemData.IsStackable)
                {
                    _eventService?.UnregisterKeyItemHandler(uniqueId, DisableItem);
                }
                else
                {
                    _eventService?.UnregisterKeyItemHandler(itemData.ItemName, DisableItem);
                }
            }
        }

        private void DisableItem()
        {
            gameObject.SetActive(false);
        }

        public string GetIdentifier()
        {
            return itemData.IsStackable ? uniqueId : itemData.ItemName;
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            // ItemDataが新しく設定された、または変更された場合
            if (itemData != null && itemData != _previousItemData)
            {
                if (itemData.IsStackable)
                {
                    // 新しいIDを生成
                    uniqueId = System.Guid.NewGuid().ToString();
                    _previousItemData = itemData;
                    
                    // シーンを保存可能な状態に
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }

            // ItemDataのスタック設定に基づいて数量を制限
            if (itemData != null)
            {
                if (itemData.IsStackable)
                {
                    quantity = Mathf.Clamp(quantity, 1, itemData.MaxStackSize);
                }
                else
                {
                    quantity = 1;
                }
            }
        }
#endif
    }
}
