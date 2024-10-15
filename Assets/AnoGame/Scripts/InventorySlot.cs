using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnoGame.Utility;
public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void SetItem(InventoryItem item)
    {
        itemImage.sprite = item.itemImage;
        itemImage.enabled = true;
        itemNameText.text = item.itemName;
        descriptionText.text = item.description;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        itemImage.sprite = null;
        itemImage.enabled = false;
        itemNameText.text = "";
        descriptionText.text = "";
        gameObject.SetActive(false);
    }
}