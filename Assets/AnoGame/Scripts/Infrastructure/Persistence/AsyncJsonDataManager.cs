using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AnoGame.Infrastructure.Persistence
{
    public class AsyncJsonDataManager
    {
        private string _basePath;

        public AsyncJsonDataManager()
        {
            // コンストラクタでは初期化しない
        }

        private void InitializePath()
        {
            if (string.IsNullOrEmpty(_basePath))
            {
                _basePath = UnityEngine.Application.persistentDataPath;
            }
        }

        public async Task SaveDataAsync<T>(string fileName, T data)
        {
            InitializePath();
            string path = Path.Combine(_basePath, fileName);
            string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            
            await File.WriteAllTextAsync(path, jsonData);
            Debug.Log($"Data saved asynchronously to JSON: {path}");
        }

        public async Task<T> LoadDataAsync<T>(string fileName) where T : class
        {
            InitializePath();
            string path = Path.Combine(_basePath, fileName);
            
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Save file not found: {path}");
                return null;
            }

            string jsonData = await File.ReadAllTextAsync(path);
            T loadedData = JsonConvert.DeserializeObject<T>(jsonData);
            
            Debug.Log($"Data loaded asynchronously from JSON: {path}");
            return loadedData;
        }

        public void SetCustomPath(string path)
        {
            _basePath = path;
        }
    }
}