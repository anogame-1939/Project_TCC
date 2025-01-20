using UnityEngine;
using UnityEngine.EventSystems;

namespace AnoGame.Application.Inventory
{
    public class InventorySlotComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        InventorySlot _inventorySlot;
        void Start()
        {
            _inventorySlot = GetComponent<InventorySlot>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            InventoryItemDetail.Instance.SetText(_inventorySlot.CurrentItem.ItemName, _inventorySlot.CurrentItem.Description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}