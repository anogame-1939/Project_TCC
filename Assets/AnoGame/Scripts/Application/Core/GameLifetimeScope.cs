using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Infrastructure.Services.Inventory;
using AnoGame.Application.Player;
using AnoGame.Infrastructure.Services;
using UnityEngine;
using AnoGame.Data;
using System.Linq;

namespace AnoGame.Application.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField]
        ItemDatabase itemDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // サービスの登録
            builder.Register<IEventService, EventService>(Lifetime.Singleton);
            builder.Register<IKeyItemService, KeyItemService>(Lifetime.Singleton);
            builder.Register<IKeyItemService>(container => 
                new KeyItemService(itemDatabase.Items.ToArray(), 
                container.Resolve<IEventService>()), 
                Lifetime.Singleton);
            // builder.Register<IGameDataService, GameDataService>(Lifetime.Singleton);

            // シングルトンへの注入を有効にする
            builder.RegisterComponentInHierarchy<GameManager>();

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();
        }
    }

}
