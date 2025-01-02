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
        private HashSet<string> _itemNames = new();

        public void SetItems(HashSet<string> itemNames)
        {
            _itemNames = itemNames;
        }
        public bool HasItem(string itemName)
        {
            return _itemNames.Contains(itemName);
        }
    }
}

