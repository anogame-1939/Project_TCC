using UnityEngine;
using VContainer;

namespace AnoGame.Application.Inventory.Test
{
    public class RemoveItemTester : MonoBehaviour
    {
        [SerializeField] private string itemName;
        [SerializeField] private int quantity = 1;

        private InventoryManager _inventoryManager;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
        }

        [ContextMenu("Test RemoveItem")]
        private void TestRemoveItem()
        {
            if (_inventoryManager == null)
            {
                Debug.LogError("InventoryManager is not injected!");
                return;
            }

            bool result = _inventoryManager.RemoveItem(itemName, quantity);
            Debug.Log($"RemoveItem({itemName}, {quantity}) => {result}");
        }
    }
}
