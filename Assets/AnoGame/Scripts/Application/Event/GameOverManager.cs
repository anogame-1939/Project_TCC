using System;
using System.Linq;
using AnoGame.Application.Core;
using AnoGame.Application.Enemy;
using AnoGame.Application.Steam;
using AnoGame.Application.Story;
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

        public void OnGameOver()
        {
            UnityEngine.Debug.Log("OnGameOver");

            GameStateManager.Instance.SetState(GameState.GameOver);
            // EnemySpawnManager.Instance.DestroyCurrentEnemyInstance();
            if (EnemySpawnManager.Instance.CurrentEnemyInstance != null)
            {
                Destroy(EnemySpawnManager.Instance.CurrentEnemyInstance);
            }

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

            // イベントをリセット (StoryManager側で処理するように変更)
            // _eventService.SetClearedEvents(GameManager2.Instance.CurrentGameData.EventHistory.ClearedEvents.ToHashSet());

            // ストーリー進行リセット処理呼び出し
            StoryManager.Instance.ResetStoryProgress();

            // イベントサービスへの反映はResetStoryProgress内でのデータ更新後に行われるべきだが、
            // 即座に反映させるためにここで再設定するか、StoryManagerがリロードする際に同期されるか確認が必要。
            // ResetStoryProgress内でデータのセーブ＆ロードを行う場合、ここのイベントセットは最新データに基づく必要がある。
        }

        public async void OnRetryGame()
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
            MessageBroker.Default.Publish(new PlayerRetriedEvent());

            // シーンをリロード
            StoryManager.Instance.ReloadStoryScene();
        }
    }
}