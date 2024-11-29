using UnityEngine;

namespace AnoGame
{
    [AddComponentMenu("Inventory/" + nameof(CollectableItem))]
    public class CollectableItem : MonoBehaviour
    {
        [SerializeField] private string itemName;
        [SerializeField] private int quantity = 1;
        [SerializeField] private string description;
        [SerializeField] private Sprite itemImage;

        public string ItemName => itemName;
        public int Quantity => quantity;
        public string Description => description;
        public Sprite ItemImage => itemImage;
    }
}