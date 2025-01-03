using System;
using System.Collections.Generic;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Infrastructure.Services
{
    /// <summary>
    /// イベントIDに対応するアクションを登録するサービス
    /// イベントの開始時、終了時のイベントを発火するだけ
    /// </summary>
    public class InventoryService : IInventoryService
    {
        public event Action<string> OnItemAdded;
        public event Action<string> OnItemRemoved;
        private HashSet<string> _itemNames = new();

        public void SetItems(HashSet<string> itemNames)
        {
            _itemNames = itemNames;
        }
        public bool HasItem(string itemName)
        {
            return _itemNames.Contains(itemName);
        }

        public void NotifyItemAdded(string itemName)
        {
            _itemNames.Add(itemName);
            OnItemAdded?.Invoke(itemName);
        }

        public void NotifyItemRemoved(string itemName)
        {
            _itemNames.Remove(itemName);
            OnItemRemoved?.Invoke(itemName);
        }
    }
}

