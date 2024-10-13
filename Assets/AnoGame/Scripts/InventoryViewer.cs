using System;
using AnoGame.Utility;
using UnityEngine;

namespace AnoGame.Inventory
{
    public class InventoryViewer : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            GameManager.Instance.SaveGameData += OnSaveGameData;
            
        }

        private void OnSaveGameData(GameData data)
        {
            // インベントリ情報を表示する


            throw new NotImplementedException();
        }

        private void CreateInventoryItem()
        {

        }
    }
}