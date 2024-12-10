using UnityEngine;
using VContainer;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Inventory.Components
{
    public class KeyDoor : MonoBehaviour
    {
        [SerializeField] private string requiredKeyName;
        [Inject] private IEventService _eventService;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
            _eventService.RegisterKeyItemHandler(requiredKeyName, OpenDoor);
        }

        private void OnDestroy()
        {
            _eventService?.UnregisterKeyItemHandler(requiredKeyName, OpenDoor);
        }

        private void OpenDoor()
        {
            Debug.Log($"Opening door that requires {requiredKeyName}");
            // ドアを開くアニメーションや処理
        }
    }
}