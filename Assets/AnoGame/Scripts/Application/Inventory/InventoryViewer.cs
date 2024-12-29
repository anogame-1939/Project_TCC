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

        private InventoryManager _inventoryManager;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
        }

        private void Start()
        {
            InitializeVisibleSlots();
            UpdatePageButtonsVisibility();
        }

        private void InitializeVisibleSlots()
        {
            for (int i = 0; i < maxVisibleSlots; i++)
            {
                InventorySlot slot = Instantiate(slotPrefab, contentParent);
                visibleSlots.Add(slot);
                // slot.Clear();
            }
        }

        public void UpdateInventory(Domain.Data.Models.Inventory inventory)
        {
            foreach (var item in inventory.Items)
            {
                if (!spriteCache.ContainsKey(item.ItemName))
                {
                    LoadSprite(item.ItemName);
                }
            }

            // 差分更新の実装
            var changes = CalculateInventoryChanges(_allItems, inventory.Items);
            ApplyInventoryChanges(changes);

            _allItems = inventory.Items; // これで型が一致する
            UpdateVisibleItems();
            UpdatePageButtonsVisibility();
        }

        private class InventoryChange
        {
            public InventoryItem Item { get; set; }
            public ChangeType Type { get; set; }
            public int OldIndex { get; set; }
            public int NewIndex { get; set; }
        }

        private enum ChangeType
        {
            Add,
            Remove,
            Modify,
            Move
        }


        private List<InventoryChange> CalculateInventoryChanges(
            IReadOnlyList<InventoryItem> oldInventory, 
            IReadOnlyList<InventoryItem> newInventory)
        {
            var changes = new List<InventoryChange>();

            // アイテムの辞書を作成（名前をキーとして使用）
            var oldItemDict = newInventory.Select((item, index) => new { Item = item, Index = index })
                                    .ToDictionary(x => x.Item.ItemName, x => x);
            var newItemDict = newInventory.Select((item, index) => new { Item = item, Index = index })
                                    .ToDictionary(x => x.Item.ItemName, x => x);

            // 削除されたアイテムを検出
            foreach (var oldItem in oldInventory)
            {
                if (!newItemDict.ContainsKey(oldItem.ItemName))
                {
                    changes.Add(new InventoryChange
                    {
                        Item = oldItem,
                        Type = ChangeType.Remove,
                        OldIndex = oldItemDict[oldItem.ItemName].Index
                    });
                }
            }

            // 追加・変更されたアイテムを検出
            foreach (var newItem in newInventory)
            {
                if (!oldItemDict.ContainsKey(newItem.ItemName))
                {
                    // 新規追加
                    changes.Add(new InventoryChange
                    {
                        Item = newItem,
                        Type = ChangeType.Add,
                        NewIndex = newItemDict[newItem.ItemName].Index
                    });
                }
                else
                {
                    var oldItem = oldInventory[oldItemDict[newItem.ItemName].Index];
                    var oldIndex = oldItemDict[newItem.ItemName].Index;
                    var newIndex = newItemDict[newItem.ItemName].Index;

                    if (newItem.Quantity != oldItem.Quantity || 
                        newItem.Description != oldItem.Description)
                    {
                        // 内容の変更
                        changes.Add(new InventoryChange
                        {
                            Item = newItem,
                            Type = ChangeType.Modify,
                            OldIndex = oldIndex,
                            NewIndex = newIndex
                        });
                    }
                    else if (oldIndex != newIndex)
                    {
                        // 位置の変更
                        changes.Add(new InventoryChange
                        {
                            Item = newItem,
                            Type = ChangeType.Move,
                            OldIndex = oldIndex,
                            NewIndex = newIndex
                        });
                    }
                }
            }

            return changes;
        }

        private void ApplyInventoryChanges(List<InventoryChange> changes)
        {
            foreach (var change in changes)
            {
                var slotIndex = change.NewIndex % maxVisibleSlots;
                var changePage = change.NewIndex / maxVisibleSlots;

                if (changePage == currentPage)
                {
                    switch (change.Type)
                    {
                        case ChangeType.Add:
                        case ChangeType.Modify:
                            if (spriteCache.TryGetValue(change.Item.ItemName, out var sprite))
                            {
                                visibleSlots[slotIndex].SetItem(change.Item, sprite);
                            }
                            else
                            {
                                // スプライトがまだロードされていない場合は、空のスプライトで設定
                                visibleSlots[slotIndex].SetItem(change.Item, null);
                                // LoadSpriteメソッドが非同期でスプライトをロードし、
                                // UpdateSlotsWithItemメソッドで後からスプライトが更新される
                            }
                            break;

                        case ChangeType.Remove:
                            if (change.OldIndex / maxVisibleSlots == currentPage)
                            {
                                visibleSlots[change.OldIndex % maxVisibleSlots].Clear();
                            }
                            break;

                        case ChangeType.Move:
                            if (change.OldIndex / maxVisibleSlots == currentPage)
                            {
                                visibleSlots[change.OldIndex % maxVisibleSlots].Clear();
                            }
                            if (spriteCache.TryGetValue(change.Item.ItemName, out var moveSprite))
                            {
                                visibleSlots[slotIndex].SetItem(change.Item, moveSprite);
                            }
                            else
                            {
                                visibleSlots[slotIndex].SetItem(change.Item, null);
                            }
                            break;
                    }
                }
            }
        }

        private void UpdateVisibleItems()
        {
            int startIndex = currentPage * maxVisibleSlots;
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
                else
                {
                    visibleSlots[i].Clear();
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