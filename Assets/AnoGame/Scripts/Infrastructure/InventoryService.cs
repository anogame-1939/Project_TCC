using System;
using System.Collections.Generic;
using AnoGame.Domain.Inventory.Services;
using UnityEngine;

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
        public event Action<ItemConsumedArgs> OnItemConsumed;


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

        public bool ConsumeItem(string itemId, int quantity = 1, GameObject user = null, Vector3? usePos = null)
        {
            throw new NotImplementedException();
        }

    }
}

