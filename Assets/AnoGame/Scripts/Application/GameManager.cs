using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnoGame.Infrastructure;
using AnoGame.Data;
using AnoGame.Application.SaveData;

namespace AnoGame.Application
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        private readonly GameDataRepository _repository;

        public Action<GameData> SaveGameData;
        public Action<GameData> LoadGameData;

        private GameData _currentGameData;
        public GameData CurrentGameData => _currentGameData;

        public GameManager()
        {
            _repository = new GameDataRepository();
        }

        private async void Start()
        {
            await InitializeGameData();
        }

        private async Task InitializeGameData()
        {
            try
            {
                GameData loadedData = await _repository.LoadDataAsync();

                if (loadedData != null)
                {
                    _currentGameData = loadedData;
                    Debug.Log("Loaded existing save data");
                }
                else
                {
                    _currentGameData = CreateNewGameData();
                    Debug.Log("Created new game data");
                }

                LoadGameData?.Invoke(_currentGameData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize game data: {ex.Message}");
                _currentGameData = CreateNewGameData();
                LoadGameData?.Invoke(_currentGameData);
            }
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

        public async Task SaveCurrentGameState()
        {
            if (_currentGameData == null) return;

            try
            {
                await _repository.SaveDataAsync(_currentGameData);
                Debug.Log("Game state saved successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game state: {ex.Message}");
            }
        }

        private async void OnApplicationQuit()
        {
            if (_currentGameData != null)
            {
                SaveGameData?.Invoke(_currentGameData);
                try
                {
                    await _repository.SaveDataAsync(_currentGameData);
                    Debug.Log("Game state saved on application quit");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save game state on quit: {ex.Message}");
                }
            }
        }

        public async Task<bool> CreateSavePoint()
        {
            if (_currentGameData == null) return false;

            try
            {
                SaveGameData?.Invoke(_currentGameData);
                await _repository.SaveDataAsync(_currentGameData);
                Debug.Log("Save point created successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create save point: {ex.Message}");
                return false;
            }
        }
    }
}