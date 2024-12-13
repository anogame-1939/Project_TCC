using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;
using System.Diagnostics;

namespace AnoGame.Application.Core
{
    public class LevelInitializer : IStartable
    {
        private readonly IKeyItemService _keyItemService;
        private readonly IItemCollectionEventService _itemCollectionService;
        private readonly GameManager _gameManager;

        public LevelInitializer(
            IKeyItemService keyItemService,
            IItemCollectionEventService itemCollectionService,
            GameManager gameManager)
        {
            _keyItemService = keyItemService;
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
                    // キーアイテムの状態復元（これは変更なし）
                    _keyItemService.RestoreKeyItemStates(new[] { item.itemName });

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
