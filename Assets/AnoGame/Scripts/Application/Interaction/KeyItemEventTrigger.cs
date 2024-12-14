using UnityEngine;
using UnityEngine.Events;
using VContainer;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Data;

namespace AnoGame.Application.Interaction.Components
{
    public class KeyItemEventTrigger : MonoBehaviour
    {
        [SerializeField] private ItemData requiredKeyItem;
        [SerializeField] private UnityEvent onKeyItemObtained;

        [Inject] private IEventService _eventService;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
            _eventService.RegisterKeyItemHandler(requiredKeyItem.ItemName, OnKeyItemObtainedHandler);
        }

        private void OnDestroy()
        {
            _eventService?.UnregisterKeyItemHandler(requiredKeyItem.ItemName, OnKeyItemObtainedHandler);
        }

        private void OnKeyItemObtainedHandler()
        {
            onKeyItemObtained?.Invoke();
        }
    }
}