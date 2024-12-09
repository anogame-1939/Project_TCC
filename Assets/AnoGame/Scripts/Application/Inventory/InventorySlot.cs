using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnoGame.Data;

namespace AnoGame.Application.Inventory
{
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI quantityText;

        public InventoryItem CurrentItem { get; private set; }

        public void SetItem(InventoryItem item, Sprite sprite)
        {
            CurrentItem = item;
            
            itemNameText.text = item.itemName;
            descriptionText.text = item.description;
            quantityText.text = item.quantity > 1 ? item.quantity.ToString() : string.Empty;
            
            UpdateSprite(sprite);
            gameObject.SetActive(true);
        }

        public void UpdateSprite(Sprite sprite)
        {
            itemImage.sprite = sprite;
            itemImage.enabled = sprite != null;
        }

        public void Clear()
        {
            CurrentItem = null;
            itemImage.sprite = null;
            itemImage.enabled = false;
            itemNameText.text = string.Empty;
            descriptionText.text = string.Empty;
            quantityText.text = string.Empty;
            gameObject.SetActive(false);
        }
    }
}