using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using AnoGame.Domain.Data.Models;
using VContainer;

namespace AnoGame.Application.Inventory
{
    public class InventoryViewer : MonoBehaviour
    {
        [SerializeField] private AnoGame.Data.ItemDatabase itemDatabase;
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AsyncOperationHandle<Sprite>> loadOperations = new Dictionary<string, AsyncOperationHandle<Sprite>>();

        [SerializeField] private InventorySlot slotPrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private int maxVisibleSlots = 16; // 4x4
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;

        private IReadOnlyList<InventoryItem> _allItems = new List<InventoryItem>();
        private List<InventorySlot> visibleSlots = new List<InventorySlot>();
        private int currentPage = 0;

        private void Start()
        {
            InitializeVisibleSlots();
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
            if (prevPageButton != null) prevPageButton.onClick.AddListener(PreviousPage);
            UpdatePageButtonsVisibility();
        }

        private void InitializeVisibleSlots()
        {
            for (int i = 0; i < maxVisibleSlots; i++)
            {
                InventorySlot slot = Instantiate(slotPrefab, contentParent);
                visibleSlots.Add(slot);
                slot.Clear();
            }
        }

        public void UpdateInventory(Domain.Data.Models.Inventory inventory)
        {
            if (inventory == null) return;

            // アイテムリストを更新
            _allItems = inventory.Items;

            // 必要なスプライトをロード
            foreach (var item in inventory.Items)
            {
                if (!spriteCache.ContainsKey(item.ItemName))
                {
                    LoadSprite(item.ItemName);
                }
            }

            // 表示を更新
            UpdateVisibleItems();
            UpdatePageButtonsVisibility();
        }

        private void UpdateVisibleItems()
        {
            int startIndex = currentPage * maxVisibleSlots;

            // すべてのスロットをクリア
            foreach (var slot in visibleSlots)
            {
                slot.Clear();
            }

            // 現在のページのアイテムを表示
            for (int i = 0; i < maxVisibleSlots; i++)
            {
                int itemIndex = startIndex + i;
                if (itemIndex < _allItems.Count)
                {
                    var item = _allItems[itemIndex];
                    if (spriteCache.TryGetValue(item.ItemName, out var sprite))
                    {
                        visibleSlots[i].SetItem(item, sprite);
                    }
                    else
                    {
                        visibleSlots[i].SetItem(item, null);
                    }
                }
            }
        }

        private void NextPage()
        {
            if ((currentPage + 1) * maxVisibleSlots < _allItems.Count)
            {
                currentPage++;
                UpdateVisibleItems();
                UpdatePageButtonsVisibility();
            }
        }

        private void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                UpdateVisibleItems();
                UpdatePageButtonsVisibility();
            }
        }

        private void UpdatePageButtonsVisibility()
        {
            if (nextPageButton != null && prevPageButton != null)
            {
                prevPageButton.interactable = (currentPage > 0);
                nextPageButton.interactable = ((currentPage + 1) * maxVisibleSlots < _allItems.Count);
            }
        }

        private async void LoadSprite(string itemName)
        {
            if (spriteCache.ContainsKey(itemName)) return;
            if (loadOperations.ContainsKey(itemName)) return;

            var itemData = itemDatabase.GetItemById(itemName);
            if (itemData == null) return;

            try
            {
                var operation = itemData.AssetReference.LoadAssetAsync<Sprite>();
                loadOperations[itemName] = operation;
                
                var sprite = await operation.Task;
                if (sprite != null)
                {
                    spriteCache[itemName] = sprite;
                    UpdateSlotsWithItem(itemName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load sprite for {itemName}: {e.Message}");
            }
            finally
            {
                loadOperations.Remove(itemName);
            }
        }

        private void UpdateSlotsWithItem(string itemName)
        {
            foreach (var slot in visibleSlots)
            {
                if (slot.CurrentItem?.ItemName == itemName && spriteCache.TryGetValue(itemName, out var sprite))
                {
                    slot.UpdateSprite(sprite);
                }
            }
        }

        private void OnDestroy()
        {
            Debug.Log("キャッシュのクリーンアップ");
            if (nextPageButton != null) nextPageButton.onClick.RemoveListener(NextPage);
            if (prevPageButton != null) prevPageButton.onClick.RemoveListener(PreviousPage);

            // キャッシュのクリーンアップ
            foreach (var operation in loadOperations.Values)
            {
                if (operation.IsValid())
                {
                    Addressables.Release(operation);
                }
            }
        }
    }
}