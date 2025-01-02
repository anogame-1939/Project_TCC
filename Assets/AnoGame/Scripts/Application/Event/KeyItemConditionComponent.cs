using UnityEngine;
using AnoGame.Domain.Event.Conditions;
using AnoGame.Domain.Inventory.Services;
using VContainer;
using AnoGame.Data;

namespace AnoGame.Application.Event
{
    public class KeyItemConditionComponent : EventConditionComponent
    {
        [Inject] private IInventoryService _inventoryService;
        [Inject]
        public void Construct(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }
        [SerializeField] private ItemData _item;

        
        
        public override IEventCondition CreateCondition()
        {
            return new KeyItemCondition(_inventoryService, _item.ItemName);
        }
    }
}