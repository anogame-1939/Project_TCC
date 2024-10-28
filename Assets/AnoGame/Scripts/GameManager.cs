using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnoGame.Core;
using AnoGame.Data;
using AnoGame.Utility;

namespace AnoGame
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        private AsyncJsonDataManager saveDataManager;
        public Action<GameData> SaveGameData;
        public Action<GameData> LoadGameData;

        private GameData currentGameData;

        private async void Start()
        {
            saveDataManager = new AsyncJsonDataManager();
            await InitializeGameData();
        }

        private async System.Threading.Tasks.Task InitializeGameData()
        {
            // Try to load existing save data
            GameData loadedData = await saveDataManager.LoadDataAsync();

            if (loadedData != null)
            {
                currentGameData = loadedData;
                Debug.Log("Loaded existing save data");
            }
            else
            {
                // Create new game data if no save exists
                currentGameData = CreateNewGameData();
                Debug.Log("Created new game data");
            }

            // Notify subscribers about the loaded/initialized data
            LoadGameData?.Invoke(currentGameData);
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

        public void UpdateGameState(Action<GameData> updateAction)
        {
            if (currentGameData != null)
            {
                updateAction?.Invoke(currentGameData);
                SaveGameData?.Invoke(currentGameData);
            }
        }

        public async void SaveCurrentGameState()
        {
            if (currentGameData != null)
            {
                await saveDataManager.SaveDataAsync(currentGameData);
                Debug.Log("Game state saved successfully");
            }
        }

        private async void OnApplicationQuit()
        {
            if (currentGameData != null)
            {
                SaveGameData?.Invoke(currentGameData); // 最終的な状態更新を取得
                await saveDataManager.SaveDataAsync(currentGameData);
                Debug.Log("Game state saved on application quit");
            }
        }

        // ゲームの状態を取得するためのヘルパーメソッド
        public GameData GetCurrentGameState()
        {
            return currentGameData;
        }

        // 明示的なセーブポイントを作成するためのメソッド
        public async void CreateSavePoint()
        {
            if (currentGameData != null)
            {
                SaveGameData?.Invoke(currentGameData);
                await saveDataManager.SaveDataAsync(currentGameData);
                Debug.Log("Save point created successfully");
            }
        }
    }
}