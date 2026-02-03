using System.Collections;
using System.Collections.Generic;
using AnoGame.Data;
using AnoGame.Domain.Inventory.Models;
using UnityEngine;
using VContainer;
using UniRx;
using AnoGame.Messages;

namespace AnoGame.Application.Inventory
{
    public class InventoryHandler : MonoBehaviour
    {
        private InventoryManager _inventoryManager;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
        }

        public void AddItem(ItemData itemData)
        {
            Debug.Log($"[InventoryHandler] AddItem called for: {itemData.ItemName}");
            if (_inventoryManager.AddItem(itemData))
            {
                Debug.Log($"[InventoryHandler] AddItem success: {itemData.ItemName}");
                // UI表示のためにイベント発行
                // UniqueIdは新規取得扱いとして適当なものを生成、もしくは空でもUI表示には影響しないはず
                var uniqueId = System.Guid.NewGuid().ToString();
                UniRx.MessageBroker.Default.Publish(new AnoGame.Messages.ItemCollected(itemData, 1, uniqueId, transform));
            }
            else
            {
                Debug.LogWarning($"[InventoryHandler] Failed to add item: {itemData.ItemName}");
            }
        }
    }
}