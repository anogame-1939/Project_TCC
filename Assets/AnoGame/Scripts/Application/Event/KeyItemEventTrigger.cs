using UnityEngine;
using AnoGame.Data;
using VContainer;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Event.Services;

namespace AnoGame.Application.Event
{
    public class KeyItemEventTrigger : EventTriggerBase
    {
        [SerializeField] private ItemData requiredKeyItem;
        
        [Inject] private IInventoryService _inventoryService;

        [Inject]
        public void Construct(IEventProgressService eventProgressService, IInventoryService inventoryService)
        {
            base.Construct(eventProgressService);
            _inventoryService = inventoryService;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                TryTriggerEvent();
            }
        }
    }
}