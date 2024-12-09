using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using AnoGame.Data;

namespace AnoGame.Application.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private ItemDatabase itemDatabase;
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AsyncOperationHandle<Sprite>> loadOperations = new Dictionary<string, AsyncOperationHandle<Sprite>>();

        // InventorySlotをシンプルな表示用クラスに変更
        public class InventorySlotData
        {
            public InventoryItem Item { get; set; }
            public Sprite Sprite { get; set; }
        }
        [SerializeField] private InventorySlot slotPrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private int maxVisibleSlots = 16; // 4x4
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;

        private List<InventoryItem> _allItems = new List<InventoryItem>();
        private List<InventorySlot> visibleSlots = new List<InventorySlot>();
        private int currentPage = 0;

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

        public void UpdateInventory(List<InventoryItem> newItems)
        {
            Debug.Log("なう");
            foreach (var item in newItems)
            {
                if (!spriteCache.ContainsKey(item.itemName))
                {
                    LoadSprite(item.itemName);
                }
            }

            // 差分更新の実装
            var changes = CalculateInventoryChanges(_allItems, newItems);
            ApplyInventoryChanges(changes);

            _allItems = new List<InventoryItem>(newItems); // 新しいリストで更新
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

        private List<InventoryChange> CalculateInventoryChanges(List<InventoryItem> oldItems, List<InventoryItem> newItems)
        {
            var changes = new List<InventoryChange>();

            // アイテムの辞書を作成（名前をキーとして使用）
            var oldItemDict = oldItems.Select((item, index) => new { Item = item, Index = index })
                                    .ToDictionary(x => x.Item.itemName, x => x);
            var newItemDict = newItems.Select((item, index) => new { Item = item, Index = index })
                                    .ToDictionary(x => x.Item.itemName, x => x);

            // 削除されたアイテムを検出
            foreach (var oldItem in oldItems)
            {
                if (!newItemDict.ContainsKey(oldItem.itemName))
                {
                    changes.Add(new InventoryChange
                    {
                        Item = oldItem,
                        Type = ChangeType.Remove,
                        OldIndex = oldItemDict[oldItem.itemName].Index
                    });
                }
            }

            // 追加・変更されたアイテムを検出
            foreach (var newItem in newItems)
            {
                if (!oldItemDict.ContainsKey(newItem.itemName))
                {
                    // 新規追加
                    changes.Add(new InventoryChange
                    {
                        Item = newItem,
                        Type = ChangeType.Add,
                        NewIndex = newItemDict[newItem.itemName].Index
                    });
                }
                else
                {
                    var oldItem = oldItems[oldItemDict[newItem.itemName].Index];
                    var oldIndex = oldItemDict[newItem.itemName].Index;
                    var newIndex = newItemDict[newItem.itemName].Index;

                    if (newItem.quantity != oldItem.quantity || 
                        newItem.description != oldItem.description)
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
                            if (spriteCache.TryGetValue(change.Item.itemName, out var sprite))
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
                            if (spriteCache.TryGetValue(change.Item.itemName, out var moveSprite))
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
                    if (spriteCache.TryGetValue(item.itemName, out var sprite))
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
                    UpdateSlotsWithItem(itemName); // 関連するスロットを更新
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
            // 表示中の該当アイテムのスロットを更新
            foreach (var slot in visibleSlots)
            {
                if (slot.CurrentItem?.itemName == itemName && spriteCache.TryGetValue(itemName, out var sprite))
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