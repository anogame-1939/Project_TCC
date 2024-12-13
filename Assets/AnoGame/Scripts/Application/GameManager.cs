using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using VContainer;
using AnoGame.Infrastructure;
using AnoGame.Data;
using AnoGame.Domain.Inventory.Services;
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

        [Inject] private IKeyItemService _keyItemService;
        [Inject] private IItemCollectionEventService _itemCollectionService;

        public GameManager()
        {
            _repository = new GameDataRepository();
        }

        [Inject]
        public void Construct(
            IKeyItemService keyItemService,
            IItemCollectionEventService itemCollectionService)
        {
            _keyItemService = keyItemService;
            _itemCollectionService = itemCollectionService;
        }

        private void Start()
        {
            InitializeGameData().Forget();
        }

        private async UniTaskVoid InitializeGameData()
        {
            try
            {
                GameData loadedData = await _repository.LoadDataAsync();

                if (loadedData != null)
                {
                    _currentGameData = loadedData;
                    Debug.Log("Loaded existing save data");
                    
                    // セーブデータからキーアイテム状態を復元
                    if (_currentGameData.inventory != null)
                    {
                        var collectedItems = _currentGameData.inventory
                            .Select(item => item.itemName)
                            .ToList();
                        
                        // キーアイテム状態を復元（これにより関連するドアなどが開く）
                        _keyItemService.RestoreKeyItemStates(collectedItems);

                        foreach (var inventory in _currentGameData.inventory)
                        {
                            if (inventory.uniqueIds.Count > 0)
                            {
                                foreach (var uniqueId in inventory.uniqueIds)
                                {
                                    _itemCollectionService.TriggerItemCollected(
                                        inventory.itemName,
                                        uniqueId
                                    );
                                }
                            }
                            else
                            {
                                _itemCollectionService.TriggerItemCollected(
                                    inventory.itemName
                                );
                            }
                        }
                    }
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

        public void UpdateGameState(GameData newGameData)
        {
            _currentGameData = newGameData;
            if (_currentGameData != null)
            {
                SaveGameData?.Invoke(_currentGameData);
            }
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