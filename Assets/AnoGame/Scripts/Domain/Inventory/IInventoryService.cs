using System.Collections.Generic;

namespace AnoGame.Domain.Inventory.Services
{
    public interface IInventoryService
    {
        void SetItems(HashSet<string> itemName);
        bool HasItem(string itemName);
    }
}

