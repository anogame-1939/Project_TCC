using System.Collections;
using System.Collections.Generic;
using AnoGame.Data;
using AnoGame.Domain.Inventory.Models;
using UnityEngine;
using VContainer;

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
            _inventoryManager.AddItem(itemData);
            
        }
    }
}