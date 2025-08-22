using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using AnoGame.Domain.Data.Models;
using VContainer;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;

namespace AnoGame.Application.Inventory
{
    public class InventoryViewer : MonoBehaviour
    {
        [SerializeField] private AnoGame.Data.ItemDatabase itemDatabase;
        [SerializeField] private InventorySlot slotPrefab;
        [Tooltip("ページごとのコンテント用 Transform をインスペクターで設定してください")]
        [SerializeField] private List<Transform> contentParents;  // ①
        [SerializeField] private int maxVisibleSlots = 16; // 4x4
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;

        private IReadOnlyList<InventoryItem> _allItems = new List<InventoryItem>();
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AsyncOperationHandle<Sprite>> loadOperations = new Dictionary<string, AsyncOperationHandle<Sprite>>();

        private List<List<InventorySlot>> pageSlots = new List<List<InventorySlot>>();  // ②
        private int currentPage = 0;

        private void Start()
        {
            InitializePages();  // ③
            if (nextPageButton != null) nextPageButton.onClick.AddListener(GoToNextPage);
            if (prevPageButton != null) prevPageButton.onClick.AddListener(GoToPreviousPage);
            UpdatePageButtonsVisibility();
            UpdateVisibleItems();
        }

        /// <summary>
        /// ページ数分のスロットを生成し、最初は 0 ページのみ表示。
        /// </summary>
        private void InitializePages()
        {
            int pageCount = contentParents.Count;
            for (int page = 0; page < pageCount; page++)
            {
                var slots = new List<InventorySlot>();
                for (int i = 0; i < maxVisibleSlots; i++)
                {
                    var slot = Instantiate(slotPrefab, contentParents[page]);  // :contentReference[oaicite:0]{index=0}
                    slot.Clear();
                    slots.Add(slot);
                }
                pageSlots.Add(slots);
                // 最初は 0 ページだけアクティブ、それ以外は非表示
                contentParents[page].gameObject.SetActive(page == currentPage);
            }
        }

        public void UpdateInventory(Domain.Data.Models.Inventory inventory)
        {
            if (inventory == null) return;

            _allItems = inventory.Items;

            // 必要なスプライトをロード
            foreach (var item in _allItems)
            {
                if (!spriteCache.ContainsKey(item.ItemName))
                    LoadSprite(item.ItemName);
            }

            UpdateVisibleItems();
            UpdatePageButtonsVisibility();
        }

        /// <summary>
        /// 現在ページのスロットだけにアイテムをセット
        /// </summary>
        private void UpdateVisibleItems()
        {
            var slots = pageSlots[currentPage];
            int startIndex = currentPage * maxVisibleSlots;

            // 全スロットクリア
            foreach (var slot in slots) slot.Clear();

            for (int i = 0; i < slots.Count; i++)
            {
                int itemIndex = startIndex + i;
                if (itemIndex < _allItems.Count)
                {
                    var item = _allItems[itemIndex];
                    if (spriteCache.TryGetValue(item.ItemName, out var sprite))
                    {
                        UniTask.Void(async () => await slots[i].SetItemAsync(item, sprite));
                    }
                    else
                    {
                        UniTask.Void(async () => await slots[i].SetItemAsync(item, null));
                    }
                }
            }

            // フォーカス更新
            var sel = slots.FirstOrDefault()?.GetComponent<Selectable>();
            if (sel != null)
            {
                EventSystem.current.SetSelectedGameObject(sel.gameObject);
                sel.Select();
            }
        }

        private void GoToNextPage()
        {
            if ((currentPage + 1) * maxVisibleSlots < _allItems.Count)
                SwitchPage(currentPage + 1);
        }

        private void GoToPreviousPage()
        {
            if (currentPage > 0)
                SwitchPage(currentPage - 1);
        }

        /// <summary>
        /// ページ切り替え時に表示／非表示をトグル
        /// </summary>
        private void SwitchPage(int newPage)
        {
            contentParents[currentPage].gameObject.SetActive(false);
            currentPage = newPage;
            contentParents[currentPage].gameObject.SetActive(true);
            UpdateVisibleItems();
            UpdatePageButtonsVisibility();
        }

        private void UpdatePageButtonsVisibility()
        {
            if (nextPageButton == null || prevPageButton == null) return;
            prevPageButton.gameObject.SetActive(currentPage > 0);
            nextPageButton.gameObject.SetActive((currentPage + 1) * maxVisibleSlots < _allItems.Count);
        }

        private async void LoadSprite(string itemName)
        {
            if (spriteCache.ContainsKey(itemName) || loadOperations.ContainsKey(itemName)) return;

            var itemData = itemDatabase.GetItemById(itemName);
            if (itemData?.AssetReference == null) return;

            try
            {
                // AssetReference からロード
                var handle = itemData.AssetReference.LoadAssetAsync<Sprite>();  // :contentReference[oaicite:1]{index=1}
                loadOperations[itemName] = handle;
                var sprite = await handle.Task;
                if (sprite != null)
                {
                    spriteCache[itemName] = sprite;
                    // 既存スロットに反映
                    UpdateSlotsWithItem(itemName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load sprite for {itemName}: {e}");
            }
            finally
            {
                // loadOperations.Remove(itemName);
            }
        }

        private void UpdateSlotsWithItem(string itemName)
        {
            foreach (var slots in pageSlots)
            {
                foreach (var slot in slots)
                {
                    if (slot.CurrentItem?.ItemName == itemName && spriteCache.TryGetValue(itemName, out var sprite))
                        slot.UpdateSprite(sprite);
                }
            }
        }

        private void OnDestroy()
        {
            if (nextPageButton != null) nextPageButton.onClick.RemoveListener(GoToNextPage);
            if (prevPageButton != null) prevPageButton.onClick.RemoveListener(GoToPreviousPage);

            foreach (var op in loadOperations.Values)
                if (op.IsValid()) Addressables.Release(op);
        }
    }
}
