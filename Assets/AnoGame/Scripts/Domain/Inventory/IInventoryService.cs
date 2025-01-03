using System;
using System.Collections.Generic;

namespace AnoGame.Domain.Inventory.Services
{
    public interface IInventoryService
    {
        event Action<string> OnItemAdded;
        event Action<string> OnItemRemoved;
        void SetItems(HashSet<string> itemNames);
        bool HasItem(string itemName);

        void NotifyItemAdded(string itemName);

        void NotifyItemRemoved(string itemName);
    }
}

