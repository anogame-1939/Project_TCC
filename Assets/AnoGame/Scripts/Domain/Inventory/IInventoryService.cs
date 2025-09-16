using System;
using System.Collections.Generic;

namespace AnoGame.Domain.Inventory.Services
{
    public interface IInventoryService
    {
        event Action<string> OnItemAdded;
        event Action<string> OnItemRemoved;
        event Action<ItemConsumedArgs> OnItemConsumed;
        void SetItems(HashSet<string> itemNames);
        bool HasItem(string itemName);
        bool ConsumeItem(string itemId, int quantity = 1,
                        UnityEngine.GameObject user = null,
                        UnityEngine.Vector3? usePos = null);

        void NotifyItemAdded(string itemName);

        void NotifyItemRemoved(string itemName);
    }
}

