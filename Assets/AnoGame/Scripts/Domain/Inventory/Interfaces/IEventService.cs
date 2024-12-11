using System.Collections.Generic;
using AnoGame.Domain.Item.Models;

namespace AnoGame.Domain.Inventory.Services
{
    public interface IEventService 
    {
        void TriggerKeyItemEvent(string itemName);
        void RegisterKeyItemHandler(string itemName, System.Action handler);
        void UnregisterKeyItemHandler(string itemName, System.Action handler);
    }

    public interface IKeyItemService
    {
        bool IsKeyItem(string itemName);
        bool IsKeyItem(IItem itemData);
        void RestoreKeyItemStates(IEnumerable<string> collectedKeyItems);
    }
}

