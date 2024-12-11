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
    public class LevelLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // KeyDoorは存在する場合のみ登録
            if (FindFirstObjectByType<KeyDoor>() != null)
            {
                builder.RegisterComponentInHierarchy<KeyDoor>();
            }
        }
    }

}
