using UnityEngine;
using VContainer;
using VContainer.Unity;
using AnoGame.Application.Event;
using AnoGame.Application.Enemy;
using AnoGame.Application.Direction.Glitch;
using AnoGame.Application.Direction.Timeline;

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

            var instantEventTriggers = FindObjectsByType<InstantEventTrigger>(FindObjectsSortMode.None);
            foreach (var trigger in instantEventTriggers)
            {
                // builder.RegisterBuildCallback(resolver => resolver.Inject(trigger));
            }

            // EventConditionComponentの登録（条件コンポーネントがある場合）
            var conditionComponents = FindObjectsByType<EventConditionComponent>(FindObjectsSortMode.None);
            foreach (var condition in conditionComponents)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(condition));
            }

            var eventOnConsumes = FindObjectsByType<EventOnConsume>(FindObjectsSortMode.None);
            foreach (var eventOnConsume in eventOnConsumes)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(eventOnConsume));
            }

            // builder.RegisterEntryPoint<LevelInitializer>();
            var proxies = FindObjectsByType<TimelineEventLockProxy>(FindObjectsSortMode.None);
            foreach (var p in proxies)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(p));
            }

            var enemyProxies = FindObjectsByType<TimelineEnemyEventLockProxy>(FindObjectsSortMode.None);
            foreach (var p in enemyProxies)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(p));
            }

            var glitchProxies = FindObjectsByType<GlitchControllerProxy>(FindObjectsSortMode.None);
            foreach (var p in glitchProxies)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(p));
            }

            var hideLimiters = FindObjectsByType<AnoGame.Application.Player.Interaction.HideSpotUsageLimiter>(FindObjectsSortMode.None);
            foreach (var limiter in hideLimiters)
            {
                builder.RegisterBuildCallback(resolver => resolver.Inject(limiter));
            }
        }
    }

}
