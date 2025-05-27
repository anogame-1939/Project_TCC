using System;
using System.Linq;
using AnoGame.Application.Core;
using AnoGame.Application.Enemy;
using AnoGame.Application.Steam;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using UniRx;
using VContainer;


namespace AnoGame.Application.Event
{
    public class GameOverManager : SingletonMonoBehaviour<GameOverManager>
    {
        public event Action GameOver;
        
        [Inject] private IInventoryService _inventoryService;
        [Inject] private IEventService _eventService;
        [Inject]
        public void Construct(
            IInventoryService inventoryService,
            IEventService eventService
        )
        {
            _inventoryService = inventoryService;
            _eventService = eventService;
        }
        
        public async void OnGameOver()
        {
            UnityEngine.Debug.Log("OnGameOver");

            GameStateManager.Instance.SetState(GameState.GameOver);
            EnemySpawnManager.Instance.DestroyCurrentEnemyInstance();

            // 主にゲームオーバー画面表示で使用
            GameOver?.Invoke();

            // ここでリトライシーンを読み込む
            ReloadData();
        }

        private async void ReloadData()
        {
            await GameManager2.Instance.ReloadDataAsync();

            // アイテムをリセット
            var itemNames = GameManager2.Instance.CurrentGameData.Inventory.Items
                            .Select(x => x.ItemName)
                            .ToHashSet();
            _inventoryService.SetItems(itemNames);
            UnityEngine.Debug.Log($"itemNames.Count:{itemNames.Count}");

            // イベントをリセット
            _eventService.SetCleadEvents(GameManager2.Instance.CurrentGameData.EventHistory.ClearedEvents.ToHashSet());
        }

        public async void OnRetryGame()
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
            MessageBroker.Default.Publish(new PlayerRetriedEvent());
        }
    }
}