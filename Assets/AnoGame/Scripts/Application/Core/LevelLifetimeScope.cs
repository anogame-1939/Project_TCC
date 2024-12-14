using UnityEngine;
using VContainer;
using VContainer.Unity;
using AnoGame.Application.Interaction.Components;
using AnoGame.Application.Inventory.Components;

namespace AnoGame.Application.Core
{
    public class LevelLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var keyDoors = FindObjectsByType<KeyDoor>(FindObjectsSortMode.None);
            var collectables = FindObjectsByType<CollectableItem>(FindObjectsSortMode.None);

            foreach (var door in keyDoors)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(door));
            }

            foreach (var item in collectables)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(item));
            }

            // キーアイテムトリガー
            var keyItemEventTriggers = FindObjectsByType<KeyItemEventTrigger>(FindObjectsSortMode.None);

            foreach (var keyItemEventTrigger in keyItemEventTriggers)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(keyItemEventTrigger));
            }

            builder.RegisterEntryPoint<LevelInitializer>();
        }
    }

}
