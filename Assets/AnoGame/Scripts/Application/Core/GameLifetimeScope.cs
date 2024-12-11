using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Infrastructure.Services.Inventory;
using AnoGame.Application.Player;
using AnoGame.Infrastructure.Services;
using UnityEngine;
using AnoGame.Data;
using System.Linq;
using AnoGame.Application.Inventory.Components;

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
                new KeyItemService(itemDatabase.Items.ToArray()), 
                Lifetime.Singleton);
            // builder.Register<IGameDataService, GameDataService>(Lifetime.Singleton);

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();
        }
    }

}
