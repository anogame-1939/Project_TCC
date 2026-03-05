using System;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Domain.Event.Conditions
{
    public class KeyItemCondition : IEventCondition, IObservableCondition, IDisposable
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _requiredItemName;
        public event Action OnConditionChanged;

        public KeyItemCondition(IInventoryService inventoryService, string requiredItemName)
        {
            _inventoryService = inventoryService;
            _requiredItemName = requiredItemName;
            _inventoryService.OnItemAdded += HandleItemAdded;
        }

        public bool IsSatisfied()
        {
            return _inventoryService.HasItem(_requiredItemName);
        }

        private void HandleItemAdded(string itemName)
        {
            if (itemName == _requiredItemName)
            {
                OnConditionChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            _inventoryService.OnItemAdded -= HandleItemAdded;
        }
    }
}