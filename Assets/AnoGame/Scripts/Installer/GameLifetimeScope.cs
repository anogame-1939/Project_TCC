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
using AnoGame.Apllication.Direction;
using AnoGame.Application.Player.Control;
using AnoGame.Application.Direction.Glitch;
using AnoGame.Application.Enemy.AI;

namespace AnoGame.Application.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField]
        ItemDatabase itemDatabase;
        [SerializeField]
        GameObject player;

        [SerializeField] private EventLockControl _eventLockControl;
        [SerializeField] private CinematicBars mainBars;
        [SerializeField] private GlitchController glitchController;

        [SerializeField] private EnemySpawnManager _enemySpawnManager;

        protected override void Configure(IContainerBuilder builder)
        {
            // サービスの登録
            // builder.Register<EventService>(Lifetime.Singleton)
            // .AsImplementedInterfaces();
            builder.RegisterComponent(_eventLockControl);
            builder.RegisterComponent(mainBars);
            builder.RegisterComponent(glitchController)
                       .As<IGlitchController>()
                       .AsSelf();
            builder.RegisterComponent(_enemySpawnManager);




            builder.Register<IGameDataRepository, GameDataRepository>(Lifetime.Singleton);
            builder.Register<IEventService, EventService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);
            builder.Register<IConsumeZoneResolver, ConsumeZoneResolver>(Lifetime.Singleton);


            // インベントリマネージャの登録
            builder.Register<InventoryManager>(Lifetime.Singleton);
            builder.Register<EventManager>(Lifetime.Singleton);

            // 
            // builder.RegisterComponent(inventoryManager);

            // セッティング系
            builder.Register<ISettingsDataRepository, SettingsDataRepository>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<SettingsManager>();

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();

            builder.RegisterComponentInHierarchy<GameManager2>();
            builder.RegisterComponentInHierarchy<GameOverManager>();

            // EventTriggerBaseを継承したコンポーネントの検索と登録
            var eventTriggers = FindObjectsByType<EventTriggerBase>(FindObjectsSortMode.None);
            foreach (var trigger in eventTriggers)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(trigger));
            }

            // 一見して何が貼っているいるのが分からないのはよくないかも
            builder.RegisterEntryPoint<LevelInitializer>();

            // PatrolRouteRegistryの登録
            var patrolRegistry = FindAnyObjectByType<PatrolRouteRegistry>();
            if (patrolRegistry != null)
            {
                builder.RegisterInstance<IPatrolRouteRegistry>(patrolRegistry);
            }

            // EnemySpawnManagerへのInject
            var spawnManager = FindAnyObjectByType<EnemySpawnManager>();
            if (spawnManager != null)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(spawnManager));
            }

            if (player != null)
            {
                if (player.TryGetComponent<IVisibleTarget>(out var visibleTarget))
                {
                    Debug.Log("EnemyStateRelay: Player found");
                    builder.RegisterInstance<IVisibleTarget>(visibleTarget);
                }
            }
        }
    }

}