using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnoGame.Domain.Data.Models;
using Localizer;
using Cysharp.Threading.Tasks;
using System;

namespace AnoGame.Application.Inventory
{
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI quantityText;

        public InventoryItem CurrentItem { get; private set; }

        public string LocalizedName = "no data";
        public string LocalizedDescription = "no data";

        public void SetItem(InventoryItem item, Sprite sprite)
        {
            CurrentItem = item;

            // LocalizationManager.GetInstance().ApplyLocalize();

            itemNameText.text = item.ItemName;
            descriptionText.text = item.Description;
            quantityText.text = item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;

            UpdateSprite(sprite);
            gameObject.SetActive(true);
        }

        public async UniTask SetItemAsync(InventoryItem item, Sprite sprite)
        {
            Debug.Log("InventorySlot-SetItemAsync");
            CurrentItem = item;

            // LocalizationManager を経由して、キーから文字列を取得
            var manager = LocalizationManager.GetInstance();
            try
            {
                LocalizedName = await manager.GetLocalizedText(item.ItemName);
                LocalizedDescription = await manager.GetLocalizedText($"{item.ItemName}.desc");
            }
            catch (Exception e)
            {
                Debug.LogError($"ローカライズテキストの取得に失敗しました。...{item.ItemName}:{e.StackTrace}");
            }

            // 意味ない
            // itemNameText.text    = localizedName;
            // descriptionText.text = localizedDesc;

            quantityText.text    = item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;

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
            if (itemImage != null) itemImage.enabled = false;
            itemNameText.text = string.Empty;
            descriptionText.text = string.Empty;
            quantityText.text = string.Empty;
            gameObject.SetActive(false);
        }
    }
}