using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnoGame.Utility
{
    public class AsyncJsonDataManager
    {
        private const string SaveFileName = "savedata.json";

        public async Task SaveDataAsync(GameData data)
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            
            await File.WriteAllTextAsync(path, jsonData);
            Debug.Log("Data saved asynchronously to JSON");
        }

        public async Task<GameData> LoadDataAsync()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            
            if (!File.Exists(path))
            {
                Debug.LogWarning("Save file not found");
                return null;
            }

            string jsonData = await File.ReadAllTextAsync(path);
            GameData loadedData = JsonConvert.DeserializeObject<GameData>(jsonData);
            
            Debug.Log("Data loaded asynchronously from JSON");
            return loadedData;
        }
    }

    [System.Serializable]
    public class GameData
    {
        public int score;
        public string playerName;
        public List<InventoryItem> inventory;
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public int quantity;
        public string description;
        public Sprite itemImage;
    }
}