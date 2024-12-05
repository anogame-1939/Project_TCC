using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using AnoGame.Data;

namespace AnoGame.Application.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
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
                            visibleSlots[slotIndex].SetItem(change.Item);
                            break;

                        case ChangeType.Remove:
                            if (change.OldIndex / maxVisibleSlots == currentPage)
                            {
                                visibleSlots[change.OldIndex % maxVisibleSlots].Clear();
                            }
                            break;

                        case ChangeType.Move:
                            // 同じページ内での移動の場合
                            if (change.OldIndex / maxVisibleSlots == currentPage)
                            {
                                visibleSlots[change.OldIndex % maxVisibleSlots].Clear();
                            }
                            visibleSlots[slotIndex].SetItem(change.Item);
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
                    visibleSlots[i].SetItem(_allItems[itemIndex]);
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
    }
}