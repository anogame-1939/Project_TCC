using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AnoGame.Infrastructure;
using AnoGame.Infrastructure.Persistence;
using AnoGame.Data;

namespace AnoGame.Application
{
    public class GameDataManager : SingletonMonoBehaviour<GameDataManager>
    {
        private AsyncJsonDataManager _jsonManager;
        private const string SaveFileName = "savedata.json";
        private GameData _currentGameData;

        public Action<GameData> SaveGameData;
        public Action<GameData> LoadGameData;
        public GameData CurrentGameData => _currentGameData;

        private async void Start()
        {
            _jsonManager = new AsyncJsonDataManager();
            await LoadGameDataAsync();
        }

        private async Task LoadGameDataAsync()
        {
            try
            {
                _currentGameData = await _jsonManager.LoadDataAsync<GameData>(SaveFileName);
                
                if (_currentGameData != null)
                {
                    LogLoadedData(_currentGameData);
                    LoadGameData?.Invoke(_currentGameData);
                }
                else
                {
                    _currentGameData = CreateDefaultGameData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game data: {ex.Message}");
                _currentGameData = CreateDefaultGameData();
            }
        }

        public async Task SaveGameDataAsync()
        {
            try
            {
                SaveGameData?.Invoke(_currentGameData);
                await _jsonManager.SaveDataAsync(SaveFileName, _currentGameData);
                Debug.Log("Game data saved successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game data: {ex.Message}");
            }
        }

        private async void OnApplicationQuit()
        {
            await SaveGameDataAsync();
        }

        private GameData CreateDefaultGameData()
        {
            return new GameData
            {
                score = 0,
                playerName = "Player1",
                inventory = new List<InventoryItem>(),
                storyProgress = new StoryProgress
                {
                    currentStoryIndex = 0,
                    currentChapterIndex = 0,
                    currentSceneIndex = 0
                }
            };
        }

        private void LogLoadedData(GameData data)
        {
            Debug.Log($"Loaded score: {data.score}, Player: {data.playerName}");
            if (data.inventory != null)
            {
                foreach (var item in data.inventory)
                {
                    Debug.Log($"Item: {item.itemName}, Quantity: {item.quantity}");
                }
            }
        }

        // 公開メソッド
        public void UpdateGameData(GameData newData)
        {
            _currentGameData = newData;
            SaveGameDataAsync().ConfigureAwait(false);
        }

        public async Task<bool> CreateSavePoint()
        {
            try
            {
                await SaveGameDataAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}