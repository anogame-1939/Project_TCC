using System.Linq;
using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Core
{
    public class LevelInitializer : IStartable
    {
        private readonly IKeyItemService _keyItemService;
        private readonly GameManager _gameManager;

        public LevelInitializer(
            IKeyItemService keyItemService,
            GameManager gameManager)
        {
            _keyItemService = keyItemService;
            _gameManager = gameManager;
        }

        void IStartable.Start()
        {
            var inventory = _gameManager.CurrentGameData?.inventory;
            if (inventory != null)
            {
                var collectedItems = inventory
                    .Select(item => item.itemName)
                    .ToList();
                
                _keyItemService.RestoreKeyItemStates(collectedItems);
            }
        }
    }

}
