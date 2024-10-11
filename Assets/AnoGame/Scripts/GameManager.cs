using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Utility
{
    public class GameManager : MonoBehaviour
    {
        private JsonAsyncSaveDataManager saveDataManager;

        private async void Start()
        {
            saveDataManager = new JsonAsyncSaveDataManager();
            
            // データのロード
            GameData loadedData = await saveDataManager.LoadDataAsync();
            if (loadedData != null)
            {
                // ロードしたデータを使用してゲーム状態を設定
                Debug.Log($"Loaded score: {loadedData.score}, Player: {loadedData.playerName}");
                foreach (var item in loadedData.inventory)
                {
                    Debug.Log($"Item: {item.itemName}, Quantity: {item.quantity}");
                }
            }
        }

        private async void OnApplicationQuit()
        {
            // ゲーム終了時にデータを保存
            GameData dataToSave = new GameData
            {
                score = 1000,
                playerName = "Player1",
                inventory = new List<InventoryItem>
                {
                    new InventoryItem { itemName = "Sword", quantity = 1 },
                    new InventoryItem { itemName = "Potion", quantity = 5 }
                }
            };
            await saveDataManager.SaveDataAsync(dataToSave);
        }
    }
}