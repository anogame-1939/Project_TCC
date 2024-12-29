using UnityEngine;
using AnoGame.Data;
using VContainer;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Application.Inventory
{
    public class InventoryManager
    {
        private readonly GameManager _gameManager;
        private readonly int _maxInventorySize = 20;

        [Inject]
        public InventoryManager(GameManager gameManager)
        {
            Debug.Log("InventoryManager initialized");
            _gameManager = gameManager;
        }

        public bool IsInventoryFull()
        {
            var inventory = _gameManager.CurrentGameData?.inventory;
            return inventory != null && inventory.Count >= _maxInventorySize;
        }

        public IReadOnlyList<InventoryItem> GetInventory()
        {
            return _gameManager.CurrentGameData?.inventory?.AsReadOnly();
        }

        public bool AddItem(CollectableItem collectableItem)
        {
            var inventory = _gameManager.CurrentGameData?.inventory;
            if (inventory == null || IsInventoryFull())
            {
                Debug.LogWarning("Inventory is full or not initialized!");
                return false;
            }

            var itemData = collectableItem.ItemData;
            var existingItem = inventory.FirstOrDefault(item => item.itemName == itemData.ItemName);

            if (existingItem != null)
            {
                UpdateExistingItem(existingItem, collectableItem);
            }
            else
            {
                AddNewItem(inventory, collectableItem);
            }

            _gameManager.UpdateGameState(_gameManager.CurrentGameData);
            return true;
        }

        private void UpdateExistingItem(InventoryItem existingItem, CollectableItem collectableItem)
        {
            existingItem.quantity += collectableItem.Quantity;
            
            if (collectableItem.ItemData.IsStackable)
            {
                existingItem.uniqueIds.Add(collectableItem.UniqueId);
            }
        }

        private void AddNewItem(List<InventoryItem> inventory, CollectableItem collectableItem)
        {
            var newItem = new InventoryItem
            {
                itemName = collectableItem.ItemData.ItemName,
                quantity = collectableItem.Quantity,
                description = collectableItem.ItemData.Description,
                uniqueIds = new List<string>()
            };

            if (collectableItem.ItemData.IsStackable)
            {
                newItem.uniqueIds.Add(collectableItem.UniqueId);
            }

            inventory.Add(newItem);
        }

        public bool RemoveItem(string itemName, int quantity = 1)
        {
            var inventory = _gameManager.CurrentGameData?.inventory;
            if (inventory == null)
            {
                Debug.LogWarning("Inventory is not initialized!");
                return false;
            }

            var existingItem = inventory.FirstOrDefault(item => item.itemName == itemName);
            if (existingItem == null || existingItem.quantity < quantity)
            {
                Debug.LogWarning($"Not enough {itemName} in inventory!");
                return false;
            }

            existingItem.quantity -= quantity;
            if (existingItem.quantity <= 0)
            {
                inventory.Remove(existingItem);
            }

            _gameManager.UpdateGameState(_gameManager.CurrentGameData);
            return true;
        }

        public int GetItemQuantity(string itemName)
        {
            var inventory = _gameManager.CurrentGameData?.inventory;
            if (inventory == null) return 0;

            var item = inventory.FirstOrDefault(i => i.itemName == itemName);
            return item?.quantity ?? 0;
        }
    }
}