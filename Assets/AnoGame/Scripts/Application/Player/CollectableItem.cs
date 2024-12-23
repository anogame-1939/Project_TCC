using VContainer;
using UnityEngine;
using AnoGame.Data;
using AnoGame.Domain.Event.Services;
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

        [Inject] private IEventProgressService _eventProgressService;

        [Inject] private IInventoryService _itemCollectionService;

        [Inject]
        public void Construct(IEventProgressService eventProgressService, IInventoryService itemCollectionService)
        {
            _eventProgressService = eventProgressService;
            _itemCollectionService = itemCollectionService;

            // アイテム収集イベントのハンドラを登録
            _itemCollectionService.RegisterItemHandler(itemData.ItemName, OnItemCollected);
        }

        private void OnDestroy()
        {
            _itemCollectionService?.UnregisterItemHandler(itemData.ItemName, OnItemCollected);
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
