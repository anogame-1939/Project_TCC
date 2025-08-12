using VContainer;
using VContainer.Unity;
using AnoGame.Domain.Data.Services;
using AnoGame.Infrastructure.SaveData;

namespace AnoGame.Application.Core
{
    public class GameManagerLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IGameDataRepository, GameDataRepository>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<GameManager2>();
        }
    }

}
