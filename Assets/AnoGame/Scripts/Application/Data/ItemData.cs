using UnityEngine;
using UnityEngine.AddressableAssets;
using AnoGame.Domain.Inventory.Models;

namespace AnoGame.Data
{
    [CreateAssetMenu(fileName = "New Item", menuName = "AnoGame/Items/Item Data")]
    public class ItemData : ScriptableObject, IItem
    {
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private AssetReference assetReference;
        [SerializeField] private ItemType itemType;  // アイテムの種類を管理するenum
        [SerializeField] private bool isStackable = true;  // スタック可能かどうか
        [SerializeField] private int maxStackSize = 99;    // 最大スタック数

        public string ItemName => itemName;
        public string Description => description;
        public AssetReference AssetReference => assetReference;
        public ItemType ItemType => itemType;
        public bool IsStackable => isStackable;
        public int MaxStackSize => maxStackSize;
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
