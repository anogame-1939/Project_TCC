using UnityEngine;
using UnityEngine.EventSystems;

namespace AnoGame.Application.Inventory
{
    // IDeselectHandler を追加
    public class InventorySlotComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        InventorySlot _inventorySlot;

        void Start()
        {
            _inventorySlot = GetComponent<InventorySlot>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _inventorySlot.ShowHighlight();
            InventoryItemDetail.Instance.SetText(_inventorySlot.LocalizedName, _inventorySlot.LocalizedDescription);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 必要ならここで detail をクリア
            _inventorySlot.HideHighlight();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_inventorySlot == null) _inventorySlot = GetComponent<InventorySlot>();
            Debug.Log($"選択された: {gameObject.name}");
            // ハイライト表示
            _inventorySlot.ShowHighlight();
            InventoryItemDetail.Instance.SetText(_inventorySlot.LocalizedName, _inventorySlot.LocalizedDescription);
        }

        // 選択が解除されたら呼ばれる
        public void OnDeselect(BaseEventData eventData)
        {
            // ハイライト非表示
            _inventorySlot.HideHighlight();
        }
    }
}
