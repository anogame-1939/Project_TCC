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
using AnoGame.Application.Settings;

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

            // セッティング系
            builder.Register<ISettingsDataRepository, SettingsDataRepository>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<SettingsManager>();

            // シングルトンへの注入を有効にする
            // builder.RegisterComponentInHierarchy<GameManager>();

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();

            // builder.Register<GameManager2>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<GameManager2>();
            builder.RegisterComponentInHierarchy<GameOverManager>();

            builder.RegisterComponentInHierarchy<EnemySpawnManager>();

            // EventTriggerBaseを継承したコンポーネントの検索と登録
            var eventTriggers = FindObjectsByType<EventTriggerBase>(FindObjectsSortMode.None);
            foreach (var trigger in eventTriggers)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(trigger));
            }
            

            builder.RegisterEntryPoint<LevelInitializer>();
            

        }
    }

}
