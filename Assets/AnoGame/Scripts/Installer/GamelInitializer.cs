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

                }
            }
        }
    }

}
