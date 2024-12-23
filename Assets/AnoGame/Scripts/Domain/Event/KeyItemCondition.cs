using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Domain.Event.Conditions
{
    public class KeyItemCondition : IEventCondition
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _requiredItemName;

        public KeyItemCondition(IInventoryService inventoryService, string requiredItemName)
        {
            _inventoryService = inventoryService;
            _requiredItemName = requiredItemName;
        }

        public bool IsSatisfied()
        {
            return _inventoryService.HasItem(_requiredItemName);
        }
    }
}