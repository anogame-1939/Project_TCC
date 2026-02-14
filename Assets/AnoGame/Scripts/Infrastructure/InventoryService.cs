using System;
using System.Collections.Generic;
using AnoGame.AnoFlow;
using AnoGame.Domain.Inventory.Services;
using UnityEngine;
using VContainer;

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

        [Inject]
        public InventoryService(IConsumeZoneResolver resolver) => _resolver = resolver;

        private readonly IConsumeZoneResolver _resolver;

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

        public bool ConsumeItem(string itemId, int quantity = 1, GameObject user = null, Vector3? usePos = null, bool consume = true)
        {
            if (!_itemNames.Contains(itemId)) return false;

            var pos = usePos ?? (user ? user.transform.position : Vector3.zero);
            if (!_resolver.TryResolve(itemId, user, pos, out var zone, out var reason))
                return false;

            var args = new ItemConsumedArgs(itemId, quantity, user, pos);
            OnItemConsumed?.Invoke(args);

            if (consume)
            {
                // ※ 数量があるなら Dictionary 化して decrement
                _itemNames.Remove(itemId);
                OnItemRemoved?.Invoke(itemId);
            }

            return true;
        }

    }
}

