using System.Collections.Generic;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Item.Models;

namespace AnoGame.Infrastructure.Services
{
    public class KeyItemService : IKeyItemService
    {
        private readonly HashSet<string> _keyItems = new();
        
        // コンストラクタでItemDataの配列を受け取る
        public KeyItemService(IItem[] keyItems)
        {
            foreach (var item in keyItems)
            {
                _keyItems.Add(item.ItemName);
            }
        }

        public bool IsKeyItem(string itemName)
        {
            return _keyItems.Contains(itemName);
        }

        public bool IsKeyItem(IItem item)
        {
            return item != null && _keyItems.Contains(item.ItemName);
        }
    }
}

