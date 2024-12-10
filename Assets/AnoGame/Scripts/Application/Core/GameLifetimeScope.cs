using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Infrastructure.Services.Inventory;
using AnoGame.Application.Player;
using AnoGame.Application.Inventory.Components;

namespace AnoGame.Application.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // サービスの登録
            builder.Register<IEventService, EventService>(Lifetime.Singleton);
            builder.Register<IKeyItemService, KeyItemService>(Lifetime.Singleton);
            // builder.Register<IGameDataService, GameDataService>(Lifetime.Singleton);

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();
            // builder.RegisterComponentInHierarchy<KeyDoor>();
        }
    }
}
