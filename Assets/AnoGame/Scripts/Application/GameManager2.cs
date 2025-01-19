using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using AnoGame.Application.Core;
using AnoGame.Domain.Data.Services;
using AnoGame.Domain.Data.Models;
using VContainer;

namespace AnoGame.Application
{
    public class GameManager2 : SingletonMonoBehaviour<GameManager2>
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _debugMode = false;
        public bool DebugMode => _debugMode;
#endif
        private bool _dataLoaded = false;
        public bool DataLoaded => _dataLoaded;

        public event Action<GameData> SaveGameData;
        public event Action<GameData> LoadGameData;
        public event Action GameOver;

        private GameData _currentGameData;
        public GameData CurrentGameData => _currentGameData;

        [Inject] private IGameDataRepository _repository;
        [Inject]
        public void Construct(IGameDataRepository repository)
        {
            _repository = repository;
            InitializeGameData().Forget();
        }

        private async UniTask InitializeGameData()
        {
            try
            {
                Debug.Log("InitializeGameData");
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

                Debug.Log($"InitializeGameData:{_currentGameData}");

                LoadGameData?.Invoke(_currentGameData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize game data: {ex.Message}");
                _currentGameData = CreateNewGameData();
                LoadGameData?.Invoke(_currentGameData);
            }

            _dataLoaded = true;
        }

        private GameData CreateNewGameData()
        {
            var position = new Position3D(21.401f, 0, -65.332f); // 初期位置
            var rotation = new Rotation3D(0, 225.071f, 0, 1); // デフォルトの回転
            var playerPosition = new PlayerPosition(
                position,
                rotation,
                "default_map", // 初期マップID
                "default_area", // 初期エリアID
                null, // 初期チェックポイントID
                null, // 初期チェックポイント位置
                null  // 初期リスポーン位置
            );

            return new GameData(
                score: 0,
                playerName: "Player1",
                storyProgress: new StoryProgress(0, 0),
                inventory: new AnoGame.Domain.Data.Models.Inventory(), // 新しい空のインベントリを作成
                position: playerPosition,
                eventHistory: new EventHistory() // 新しい空のイベント履歴を作成
            );
        }


        public async UniTask ReloadDataAsync()
        {
            await InitializeGameData();
        }

        /// <summary>
        /// データを再読み込み
        /// 主にゲームオーバー時に使用
        /// </summary>
        public void ResetDataAsync()
        {
            _currentGameData = CreateNewGameData();
            // LoadGameData?.Invoke(_currentGameData);
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

        public void InvokeGameOver()
        {
            Debug.Log("InvokeGameOver");
            GameOver?.Invoke();
        }
    }
}