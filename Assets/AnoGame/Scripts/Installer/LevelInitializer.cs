using VContainer.Unity;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using System.Linq;
using AnoGame.Domain.Data.Models;
namespace AnoGame.Application.Core
{
    public class LevelInitializer : IStartable
    {
        // private readonly IInventoryService _itemCollectionService;
        private readonly GameManager2 _gameManager;
        private readonly IEventService _eventService;
        private readonly IInventoryService _inventoryService;

        public LevelInitializer(
            // IInventoryService itemCollectionService,
            GameManager2 gameManager,
            IEventService eventService,
            IInventoryService inventoryService
            )
        {
            UnityEngine.Debug.Log("LevelInitializer.Constructor()");
            _gameManager = gameManager;
            _eventService = eventService;
            _inventoryService = inventoryService;
        }

        void IStartable.Start()
        {
            UnityEngine.Debug.Log("LevelInitializer.Start()");
            _gameManager.LoadGameData += SetUp;




            UnityEngine.Debug.Log("LevelInitializer.Start().END");
        }

        private void SetUp(GameData gameData)
        {
            if (_gameManager.CurrentGameData.EventHistory != null)
            {
                _eventService.SetCleadEvents(_gameManager.CurrentGameData.EventHistory.ClearedEvents.ToHashSet());

            }

            var itemNames = _gameManager.CurrentGameData.Inventory.Items
                            .Select(x => x.ItemName);

            _inventoryService.SetItems(itemNames.ToHashSet());
        }

    }

}
