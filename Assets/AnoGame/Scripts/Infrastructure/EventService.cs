using System;
using System.Linq;
using System.Collections.Generic;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;

// Infrastructure層の実装
namespace AnoGame.Infrastructure.Services.Inventory
{
    public class EventService : IEventService, IItemCollectionEventService
    {
        // 既存のキーアイテムイベント用
        private readonly Dictionary<string, List<Action>> _keyItemHandlers = new();
        
        // 新しいアイテム収集イベント用
        private readonly Dictionary<string, List<Action<string>>> _itemCollectionHandlers = new();

        // IEventServiceの実装（既存）
        public void TriggerKeyItemEvent(string itemName)
        {
            if (_keyItemHandlers.TryGetValue(itemName, out var handlers))
            {
                foreach (var handler in handlers.ToList())
                {
                    handler?.Invoke();
                }
            }
        }

        public void RegisterKeyItemHandler(string itemName, Action handler)
        {
            if (!_keyItemHandlers.ContainsKey(itemName))
            {
                _keyItemHandlers[itemName] = new List<Action>();
            }
            _keyItemHandlers[itemName].Add(handler);
        }

        public void UnregisterKeyItemHandler(string itemName, Action handler)
        {
            if (_keyItemHandlers.ContainsKey(itemName))
            {
                _keyItemHandlers[itemName].Remove(handler);
            }
        }

        public void TriggerItemCollected(string itemName, string uniqueId = null)
        {
            if (_itemCollectionHandlers.TryGetValue(itemName, out var handlers))
            {
                foreach (var handler in handlers.ToList())
                {
                    handler?.Invoke(uniqueId);
                }
            }
        }

        public void RegisterItemHandler(string itemName, Action<string> handler)
        {
            if (!_itemCollectionHandlers.ContainsKey(itemName))
            {
                _itemCollectionHandlers[itemName] = new List<Action<string>>();
            }
            _itemCollectionHandlers[itemName].Add(handler);
        }

        public void UnregisterItemHandler(string itemName, Action<string> handler)
        {
            if (_itemCollectionHandlers.ContainsKey(itemName))
            {
                _itemCollectionHandlers[itemName].Remove(handler);
            }
        }
    }
}

