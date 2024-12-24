using VContainer;
using UnityEngine;
using AnoGame.Data;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Application.Event;
using AnoGame.Domain.Event;

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


        [SerializeField] private EventTriggerBase relatedEventTrigger;
        
        public string GetIdentifier()
        {
            return itemData.IsStackable ? uniqueId : itemData.ItemName;
        }

        public bool CanCollect()
        {
            // TODO:直す
            Debug.LogWarning("CanCollect…とりあえずtrue返しちゃってるよ");
            return true;
        }
        
        public void OnCollected()
        {
            if (!CanCollect()) return;
            
            // _inventoryService.AddItem(itemData.ItemName, quantity);
            gameObject.SetActive(false);
            
            // 関連イベントが設定されている場合はイベントを開始
            if (relatedEventTrigger != null)
            {
                relatedEventTrigger.StartEvent();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // ItemDataが新しく設定された、または変更された場合
            if (itemData != null && itemData != _previousItemData)
            {
                _previousItemData = itemData;  // 先に前回値を更新

                if (itemData.IsStackable && string.IsNullOrEmpty(uniqueId))
                {
                    // ユニークIDが未設定の場合のみ新しいIDを生成
                    uniqueId = System.Guid.NewGuid().ToString();
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
