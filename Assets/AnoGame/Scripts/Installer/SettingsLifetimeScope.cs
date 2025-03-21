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

using AnoGame.Application.Settings;

namespace AnoGame.Application.Core
{
    public class SettingsLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISettingsDataRepository, SettingsDataRepository>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<SettingsManager>();

            

        }
    }

}
