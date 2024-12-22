using System.Collections.Generic;
using AnoGame.Domain.Item.Models;

namespace AnoGame.Domain.Inventory.Services
{
    public interface IKeyItemService
    {
        bool IsKeyItem(string itemName);
        bool IsKeyItem(IItem itemData);
        void RestoreKeyItemStates(IEnumerable<string> collectedKeyItems);
    }
}

