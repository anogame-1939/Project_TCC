// Presentation/Debug/DebugInventoryUI.cs
using UnityEngine;
using VContainer;
using AnoGame.Data;
using AnoGame.Application.Inventory; // ★ InventoryManager

namespace AnoGame.SLFBDebug
{
    [AddComponentMenu("AnoGame/Debug/DebugInventoryUI")]
    public class DebugInventoryUI : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private Transform listParent;            // VerticalLayoutGroupなどを持つ親
        [SerializeField] private DebugInventoryRow rowPrefab;     // 行コンポーネントのプレハブ

        [Header("Data")]
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Row Options")]
        [SerializeField] private bool showIdInLabel = true;
        [SerializeField, Min(1)] private int consumeQuantity = 1;

        private InventoryManager _inventoryManager; // ★ ここもInventoryManagerに

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
            Debug.Log("[DebugInventoryUI] Constructed via DI (InventoryManager)");
        }

        private void Start()
        {
            if (itemDatabase == null)
            {
                Debug.LogError("[DebugInventoryUI] ItemDatabaseが未設定です。");
                return;
            }
            if (listParent == null || rowPrefab == null)
            {
                Debug.LogError("[DebugInventoryUI] listParent または rowPrefab が未設定です。");
                return;
            }

            BuildList();
        }

        private void BuildList()
        {
            // 既存クリア
            for (int i = listParent.childCount - 1; i >= 0; i--)
                Destroy(listParent.GetChild(i).gameObject);

            foreach (var item in itemDatabase.Items)
            {
                var row = Instantiate(rowPrefab, listParent, false);
                row.Initialize(
                    inventoryManager: _inventoryManager,
                    item: item,
                    quantity: consumeQuantity,
                    showIdInLabel: showIdInLabel
                );
            }
        }
    }
}
