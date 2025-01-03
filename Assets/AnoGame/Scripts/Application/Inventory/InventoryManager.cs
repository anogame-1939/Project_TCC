using UnityEngine;
using VContainer;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Domain.Data.Models;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Inventory
{
    public class InventoryManager
    {
        private readonly GameManager2 _gameManager;
        private readonly IInventoryService _inventoryService;
        private readonly int _maxInventorySize = 20;

        [Inject]
        public InventoryManager(
            GameManager2 gameManager, 
            IInventoryService inventoryService)
        {
            _gameManager = gameManager;
            _inventoryService = inventoryService;
            // _maxInventorySize = maxInventorySize;
        }

        public bool IsInventoryFull()
        {
            var inventory = _gameManager.CurrentGameData?.Inventory;
            return inventory != null && inventory.Items.Count >= _maxInventorySize;
        }

        public IReadOnlyList<InventoryItem> GetInventory()
        {
            return _gameManager.CurrentGameData?.Inventory?.Items;
        }

        public bool AddItem(CollectableItem collectableItem)
        {
            var inventory = _gameManager.CurrentGameData?.Inventory;
            if (inventory == null || IsInventoryFull())
            {
                Debug.LogWarning("Inventory is full or not initialized!");
                return false;
            }

            var itemData = collectableItem.ItemData;
            var existingItem = inventory.Items.FirstOrDefault(item => item.ItemName == itemData.ItemName);

            if (existingItem != null)
            {
                existingItem.AddQuantity(collectableItem.Quantity);
            }
            else
            {
                AddNewItem(inventory, collectableItem);
            }

            _gameManager.UpdateGameState(_gameManager.CurrentGameData);
            _inventoryService.NotifyItemAdded(itemData.ItemName);
            return true;
        }

        private void AddNewItem(Domain.Data.Models.Inventory inventory, CollectableItem collectableItem)
        {
            var newItem = new InventoryItem(
                itemName: collectableItem.ItemData.ItemName,
                quantity: collectableItem.Quantity,
                description: collectableItem.ItemData.Description,
                uniqueId: collectableItem.UniqueId
            );

            inventory.AddItem(newItem);
        }

        public bool RemoveItem(string itemName, int quantity = 1)
        {
            var inventory = _gameManager.CurrentGameData?.Inventory;
            if (inventory == null)
            {
                Debug.LogWarning("Inventory is not initialized!");
                return false;
            }

            var existingItem = inventory.Items.FirstOrDefault(item => item.ItemName == itemName);
            if (existingItem == null || existingItem.Quantity < quantity)
            {
                Debug.LogWarning($"Not enough {itemName} in inventory!");
                return false;
            }

            // 完全に削除する場合
            if (existingItem.Quantity <= quantity)
            {
                inventory.RemoveItem(existingItem.UniqueId);
                _inventoryService.NotifyItemRemoved(existingItem.ItemName);
            }
            else
            {
                // 数量を減らす
                existingItem.AddQuantity(-quantity);
            }

            _gameManager.UpdateGameState(_gameManager.CurrentGameData);
            return true;
        }

        public int GetItemQuantity(string itemName)
        {
            var inventory = _gameManager.CurrentGameData?.Inventory;
            if (inventory == null) return 0;

            var item = inventory.Items.FirstOrDefault(i => i.ItemName == itemName);
            return item?.Quantity ?? 0;
        }
    }
}