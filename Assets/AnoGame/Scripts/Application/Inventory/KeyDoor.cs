using UnityEngine;
using VContainer;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Item.Models;
using AnoGame.Data;

namespace AnoGame.Application.Inventory.Components
{
    public class KeyDoor : MonoBehaviour
    {
        [SerializeField] private ItemData requiredKeyItem;
        public IItem RequiredKeyItem => requiredKeyItem;

        [Inject] private IEventService _eventService;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
            _eventService.RegisterKeyItemHandler(requiredKeyItem.ItemName, OpenDoor);
        }

        private void OnDestroy()
        {
            _eventService?.UnregisterKeyItemHandler(requiredKeyItem.ItemName, OpenDoor);
        }

        private void OpenDoor()
        {
            Debug.Log($"Opening door that requires {requiredKeyItem.ItemName}");
            // ドアを開くアニメーションや処理
        }
    }
}