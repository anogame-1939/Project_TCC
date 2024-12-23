using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Event.Services;
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
            builder.Register<EventService>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // KeyItemServiceの重複登録を削除
            builder.Register<IKeyItemService>(container => 
                new KeyItemService(itemDatabase.Items.ToArray(), 
                container.Resolve<IEventService>()), 
                Lifetime.Singleton);


            // シングルトンへの注入を有効にする
            builder.RegisterComponentInHierarchy<GameManager>();

            // コンポーネントの登録
            builder.RegisterComponentInHierarchy<ItemCollector>();

            // IEventProgressServiceの登録
            builder.Register<IEventProgressService, EventProgressService>(Lifetime.Singleton);

        }
    }

}
