using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AnoGame.Application.Core;
using AnoGame.Data;
using AnoGame.Domain.Data.Services;
using VContainer;

namespace AnoGame.Application
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _debugMode = false;
        public bool DebugMode => _debugMode;
#endif

        public Action<GameData> SaveGameData;
        public Action<GameData> LoadGameData;

        private GameData _currentGameData;
        public GameData CurrentGameData => _currentGameData;

        [Inject] private readonly IGameDataRepository _repository;
        [Inject]
        public GameManager(IGameDataRepository gameDataManager)
        {
            _repository = gameDataManager;
        }

        private void Start()
        {
            // InitializeGameData().Forget();
        }

        private async UniTaskVoid InitializeGameData()
        {
            try
            {
                GameData loadedData = null;

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
                },
                playerPosition = new PlayerPositionData()
            };
        }

        /// <summary>
        /// データを再読み込み
        /// 主にゲームオーバー時に使用
        /// </summary>
        public void ReloadData()
        {
            InitializeGameData().Forget();
        }

        public void UpdateGameState(GameData newGameData)
        {
            _currentGameData = newGameData;
            if (_currentGameData != null)
            {
                SaveGameData?.Invoke(_currentGameData);
            }
        }

        public void AddItem()
        {

        }

        public void SaveData()
        {
            SaveCurrentGameState().SuppressCancellationThrow().Forget();
        }

        public async UniTask SaveCurrentGameState()
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

        private void OnApplicationQuit()
        {
#if UNITY_EDITOR
            if (_debugMode)
            {
                if (_currentGameData != null)
                {
                    SaveGameData?.Invoke(_currentGameData);
                    try
                    {
                        // アプリケーション終了時は同期的に保存を実行
                        SaveCurrentGameState().SuppressCancellationThrow().Forget();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to save game state on quit: {ex.Message}");
                    }
                }
            }
#endif
        }

        public async UniTask<bool> CreateSavePoint()
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