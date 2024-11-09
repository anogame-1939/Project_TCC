using UnityEngine;
using UnityEngine.UI;
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
            // nextPageButton.onClick.AddListener(NextPage);
            // prevPageButton.onClick.AddListener(PreviousPage);
            UpdatePageButtonsVisibility();
        }

        private void InitializeVisibleSlots()
        {
            for (int i = 0; i < maxVisibleSlots; i++)
            {
                InventorySlot slot = Instantiate(slotPrefab, contentParent);
                visibleSlots.Add(slot);
                slot.Clear(); // 初期状態はクリア
            }
        }

        public void UpdateInventory(List<InventoryItem> newItems)
        {
            _allItems = newItems;
            currentPage = 0; // 更新時は最初のページに戻る
            UpdateVisibleItems();
            UpdatePageButtonsVisibility();
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
            return;
            prevPageButton.interactable = (currentPage > 0);
            nextPageButton.interactable = ((currentPage + 1) * maxVisibleSlots < _allItems.Count);
        }
    }
}