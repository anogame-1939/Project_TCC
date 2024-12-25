using UnityEngine;
using VContainer;
using VContainer.Unity;
using AnoGame.Application.Event;
using AnoGame.Application.Enemy;


namespace AnoGame.Application.Core
{
    public class LevelLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {

            var collectables = FindObjectsByType<CollectableItem>(FindObjectsSortMode.None);
            foreach (var item in collectables)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(item));
            }

            // EventTriggerBaseを継承したコンポーネントの検索と登録
            var eventTriggers = FindObjectsByType<EventTriggerBase>(FindObjectsSortMode.None);
            foreach (var trigger in eventTriggers)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(trigger));
            }

            // EnemyEventControllerの登録
            var enemyControllers = FindObjectsByType<EnemyEventController>(FindObjectsSortMode.None);
            foreach (var controller in enemyControllers)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(controller));
            }

            // EventConditionComponentの登録（条件コンポーネントがある場合）
            var conditionComponents = FindObjectsByType<EventConditionComponent>(FindObjectsSortMode.None);
            foreach (var condition in conditionComponents)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(condition));
            }

            builder.RegisterEntryPoint<LevelInitializer>();
        }
    }

}
