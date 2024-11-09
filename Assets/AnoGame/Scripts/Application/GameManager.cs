using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnoGame.Infrastructure;
using AnoGame.Data;
using AnoGame.Utility;

namespace AnoGame.Application
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        private AsyncJsonDataManager _saveDataManager;
        public Action<GameData> SaveGameData;
        public Action<GameData> LoadGameData;

        private GameData _currentGameData;
        public GameData CurrentGameData => _currentGameData;

        private async void Start()
        {
            _saveDataManager = new AsyncJsonDataManager();
            await InitializeGameData();
        }

        private async System.Threading.Tasks.Task InitializeGameData()
        {
            // Try to load existing save data
            GameData loadedData = await _saveDataManager.LoadDataAsync();

            if (loadedData != null)
            {
                _currentGameData = loadedData;
                Debug.Log("Loaded existing save data");
            }
            else
            {
                // Create new game data if no save exists
                _currentGameData = CreateNewGameData();
                Debug.Log("Created new game data");
            }

            // Notify subscribers about the loaded/initialized data
            LoadGameData?.Invoke(_currentGameData);
        }

        private GameData CreateNewGameData()
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

        public void UpdateGameState(GameData newGameData)
        {
            _currentGameData = newGameData;
            if (_currentGameData != null)
            {
                SaveGameData?.Invoke(_currentGameData);
            }
        }

        public async void SaveCurrentGameState()
        {
            if (_currentGameData != null)
            {
                await _saveDataManager.SaveDataAsync(_currentGameData);
                Debug.Log("Game state saved successfully");
            }
        }

        private async void OnApplicationQuit()
        {
            if (_currentGameData != null)
            {
                SaveGameData?.Invoke(_currentGameData); // 最終的な状態更新を取得
                await _saveDataManager.SaveDataAsync(_currentGameData);
                Debug.Log("Game state saved on application quit");
            }
        }

        // ゲームの状態を取得するためのヘルパーメソッド
        public GameData GetCurrentGameState()
        {
            return _currentGameData;
        }

        // 明示的なセーブポイントを作成するためのメソッド
        public async void CreateSavePoint()
        {
            if (_currentGameData != null)
            {
                SaveGameData?.Invoke(_currentGameData);
                await _saveDataManager.SaveDataAsync(_currentGameData);
                Debug.Log("Save point created successfully");
            }
        }
    }
}