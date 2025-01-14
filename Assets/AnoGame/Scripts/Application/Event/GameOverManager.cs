using System.Diagnostics;
using System.Linq;
using AnoGame.Application.Core;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using VContainer;


namespace AnoGame.Application.Event
{
    public class GameOverManager : SingletonMonoBehaviour<GameOverManager>
    {
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
            GameManager2.Instance.InvokeGameOver();

            return;

            // データをリロード
            await GameManager2.Instance.ReloadDataAsync();

            // アイテムをリセット
            var itemNames = GameManager2.Instance.CurrentGameData.Inventory.Items
                            .Select(x => x.ItemName)
                            .ToHashSet();
            _inventoryService.SetItems(itemNames);
            UnityEngine.Debug.Log($"itemNames.Count:{itemNames.Count}");

            // イベントをリセット
            _eventService.SetCleadEvents(GameManager2.Instance.CurrentGameData.EventHistory.ClearedEvents.ToHashSet());



            // TODO:イベント公開する
        }

        public async void OnRetryGame()
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
    }
}