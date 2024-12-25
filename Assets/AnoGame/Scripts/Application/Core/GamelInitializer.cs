using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Core
{
    public class GamelInitializer : IStartable
    {
        private readonly IInventoryService _itemCollectionService;
        private readonly GameManager _gameManager;

        public GamelInitializer(
            IInventoryService itemCollectionService,
            GameManager gameManager)
        {
            _itemCollectionService = itemCollectionService;
            _gameManager = gameManager;
        }

        void IStartable.Start()
        {
            var inventory = _gameManager.CurrentGameData?.inventory;
            if (inventory != null)
            {
                foreach (var item in inventory)
                {

                    // スタック可能アイテムは各ユニークIDごとにイベント発火
                    if (item.uniqueIds != null && item.uniqueIds.Count > 0)
                    {
                        foreach (var uniqueId in item.uniqueIds)
                        {
                            UnityEngine.Debug.Log($"itemData:{item.itemName}, uniqueId:{uniqueId}");
                            _itemCollectionService.TriggerItemCollected(item.itemName, uniqueId);
                        }
                    }
                    else
                    {
                        // 非スタック可能アイテムは通常通り発火
                        UnityEngine.Debug.Log($"itemData:{item.itemName}, non-stackable");
                        _itemCollectionService.TriggerItemCollected(item.itemName, null);
                    }
                }
            }
        }
    }

}
