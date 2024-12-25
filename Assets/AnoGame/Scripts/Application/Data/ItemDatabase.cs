using UnityEngine;
using System.Collections.Generic;

namespace AnoGame.Data
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "AnoGame/Items/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;

        public ItemData GetItemById(string itemName)
        {
            return items.Find(item => item.ItemName == itemName);
        }
    }
}