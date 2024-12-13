using VContainer;
using VContainer.Unity;
using AnoGame.Application.Inventory.Components;

namespace AnoGame.Application.Core
{
    public class LevelLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // KeyDoorは存在する場合のみ登録
            if (FindFirstObjectByType<KeyDoor>() != null)
            {
                builder.RegisterComponentInHierarchy<KeyDoor>();
            }

            // このスコープの初期化完了時のコールバックを登録
            builder.RegisterEntryPoint<LevelInitializer>();
        }
    }

}
