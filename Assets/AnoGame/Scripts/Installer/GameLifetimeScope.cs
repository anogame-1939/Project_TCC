using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Player;
using AnoGame.Application.Enemy;
using AnoGame.Infrastructure.Services;
using UnityEngine;
using AnoGame.Data;
using AnoGame.Domain.Data.Services;
using AnoGame.Infrastructure.SaveData;

using AnoGame.Application.Inventory;
using AnoGame.Application.Event;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField]
        ItemDatabase itemDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // サービスの登録
            // builder.Register<EventService>(Lifetime.Singleton)
               // .AsImplementedInterfaces();

           

            
            builder.Register<IGameDataRepository, GameDataRepository>(Lifetime.Singleton);
            builder.Register<IEventService, EventService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);
            builder.Register<GameManager>(Lifetime.Singleton);

            // インベントリマネージャの登録
            builder.Register<InventoryManager>(Lifetime.Singleton);
            builder.Register<EventManager>(Lifetime.Singleton);

            // 
            // builder.RegisterComponent(inventoryManager);



            // シングルトンへの注入を有効にする
            // builder.RegisterComponentInHierarchy<GameManager>();

            // コンポーネントの登録
            // builder.RegisterComponentInHierarchy<ItemCollector>();

            // builder.Register<GameManager2>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<GameManager2>();
            builder.RegisterComponentInHierarchy<EnemySpawnManager>();

            builder.RegisterEntryPoint<LevelInitializer>();
            
        }
    }

}
