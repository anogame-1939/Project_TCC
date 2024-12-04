using UnityEngine;

namespace AnoGame
{
    [AddComponentMenu("Inventory/" + nameof(CollectableItem))]
    public class CollectableItem : MonoBehaviour
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;

        public ItemData ItemData => itemData;
        public int Quantity => quantity;

        private void OnValidate()
        {
            // ItemDataが設定されている場合、quantityの値をチェック
            if (itemData != null)
            {
                if (itemData.IsStackable)
                {
                    quantity = Mathf.Clamp(quantity, 1, itemData.MaxStackSize);
                }
                else
                {
                    quantity = 1;
                }
            }
        }
    }
}
