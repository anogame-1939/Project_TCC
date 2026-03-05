using AnoGame.Application.Core;
using TMPro;
using UnityEngine;

namespace AnoGame.Application.Inventory
{
    public class InventoryItemDetail : SingletonMonoBehaviour<InventoryItemDetail>
    {
        [SerializeField]
        TMP_Text _itemName;
        [SerializeField]
        TMP_Text _itemDescription;

        public void SetText(string itemName, string itemDescription)
        {
            _itemName.text = itemName;
            _itemDescription.text = itemDescription;
        }


    }
}