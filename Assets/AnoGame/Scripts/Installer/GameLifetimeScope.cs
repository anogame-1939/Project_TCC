using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Player;
using AnoGame.Application.Enemy;
using AnoGame.Infrastructure.Services;
using UnityEngine;
using AnoGame.Data;

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

            builder.Register<IEventService, EventService>(Lifetime.Singleton);
            builder.Register<GameManager>(Lifetime.Singleton);






            // シングルトンへの注入を有効にする
            // builder.RegisterComponentInHierarchy<GameManager>();

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();

            builder.RegisterComponentInHierarchy<EnemySpawnManager>();
            

            // IEventProgressServiceの登録
            builder.Register<IEventProgressService, EventProgressService>(Lifetime.Singleton);

        }
    }

}
