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
        [SerializeField] private EventTriggerBase[] relatedEventTriggers;
        
        // イベントの進捗状況を管理
        [Inject] private IEventProgressService _eventProgressService;

        // インベントを管理
        [Inject] private IInventoryService _inventoryService;

        [Inject]
        public void Construct(IEventProgressService eventProgressService, IInventoryService itemCollectionService)
        {
            _eventProgressService = eventProgressService;
            _inventoryService = itemCollectionService;

            // アイテム収集イベントのハンドラを登録
            _inventoryService.RegisterItemHandler(itemData.ItemName, OnItemCollected);
        }

        private void OnDestroy()
        {
            _inventoryService?.UnregisterItemHandler(itemData.ItemName, OnItemCollected);
        }

        private void OnItemCollected(string collectedUniqueId)
        {
            Debug.Log($"itemData:{itemData.ItemName}, {uniqueId}, {collectedUniqueId}");
            if (itemData.IsStackable)
            {
                // スタック可能なアイテムはユニークIDが一致する場合のみ非表示
                if (uniqueId == collectedUniqueId)
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                // スタック不可のアイテムは同じ種類なら全て非表示
                gameObject.SetActive(false);
            }
        }

        public string GetIdentifier()
        {
            return itemData.IsStackable ? uniqueId : itemData.ItemName;
        }

        public bool CanCollect()
        {
            // relatedEventTriggerが設定されている場合、イベントの状態をチェック
            if (relatedEventTrigger != null)
            {
                var eventState = _eventProgressService.GetEventState(relatedEventTrigger.EventData.EventId);
                // イベント完了済みの場合は取得不可
                if (eventState == EventState.Completed)
                {
                    return false;
                }
            }

            // 既に所持している場合は取得不可
            if (_inventoryService.HasItem(itemData.ItemName))
            {
                return false;
            }

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
                _eventProgressService.StartEvent(relatedEventTrigger.EventData.EventId);
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
