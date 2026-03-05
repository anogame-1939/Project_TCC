using VContainer;
using VContainer.Unity;


namespace AnoGame.Application.Core
{
    public class DebugLifetimeScope : LifetimeScope
    {

        protected override void Configure(IContainerBuilder builder)
        {
            // サービスの登録
            // builder.Register<EventService>(Lifetime.Singleton)
            // .AsImplementedInterfaces();
        }
    }

}
