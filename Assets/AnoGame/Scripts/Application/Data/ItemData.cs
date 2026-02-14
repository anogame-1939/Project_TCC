using UnityEngine;
using UnityEngine.AddressableAssets;
using AnoGame.Domain.Inventory.Models;
using System.Collections.Generic;

namespace AnoGame.Data
{
    [CreateAssetMenu(fileName = "New Item", menuName = "AnoGame/Items/Item Data")]
    public class ItemData : ScriptableObject, IItem
    {
        [SerializeField] private string itemId; // 安定ID（Inspectorで設定 or 自動生成）
        public string ItemId => itemId;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private AssetReference assetReference;
        [SerializeField] private ItemType itemType;  // アイテムの種類を管理するenum
        [SerializeField] private bool isStackable = true;  // スタック可能かどうか
        [SerializeField] private int maxStackSize = 99;    // 最大スタック数
        [SerializeField] private bool isConsumable = true; // 消費するかどうか

        [Header("メタデータ（デバッグ/フィルタ用）")]
        [SerializeField] private Episode episode;               // エピソード
        [SerializeField] private List<string> tags = new();     // フィルタ用タグ

        public string ItemName => itemName;
        public string Description => description;
        public AssetReference AssetReference => assetReference;
        public ItemType ItemType => itemType;
        public bool IsStackable => isStackable;
        public int MaxStackSize => maxStackSize;
        public bool IsConsumable => isConsumable;
        public Episode Episode => episode;
        public IReadOnlyList<string> Tags => tags;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 先頭と末尾に付いたダブルクォートを削除
            if (!string.IsNullOrEmpty(description))
            {
                description = description.Trim('"');
            }
        }
#endif
    }

    public enum ItemType
    {
        Consumable,
        Equipment,
        Material,
        Quest,
        // 必要に応じて追加
    }
}
