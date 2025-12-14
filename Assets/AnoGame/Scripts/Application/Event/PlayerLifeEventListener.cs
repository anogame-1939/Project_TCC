using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using VContainer;
using AnoGame.Domain.Event;
using AnoGame.Domain.Event.Conditions;
using AnoGame.Domain.Event.Services;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// Listens for Player life events (Miss, Death) and triggers UnityEvents.
    /// This allows designers to hook up effects and game over logic in the Inspector.
    /// </summary>
    public class PlayerLifeEventListener : MonoBehaviour
    {
        [System.Serializable]
        public class SpecialMissScenario
        {
            [Tooltip("このシナリオの名前（インスペクターでの識別用）")]
            public string Name;

            [Tooltip("この特別シナリオが発生するために必要な条件")]
            public EventConditionComponent[] Conditions;

            [Tooltip("条件が満たされた場合に実行されるイベント")]
            public UnityEvent OnMiss;

            [Tooltip("trueの場合、通常のOnMissイベントは実行されません")]
            public bool BlockStandardMiss = true;
        }

        [Header("Events")]
        [Tooltip("プレイヤーがダメージを受けたが生存している場合にトリガーされます")]
        public UnityEvent OnMiss;

        [Tooltip("プレイヤーが死亡した（ライフが0以下になった）場合にトリガーされます")]
        public UnityEvent OnDeath;

        [Header("Special Scenarios")]
        [SerializeField]
        private List<SpecialMissScenario> _specialMissScenarios = new List<SpecialMissScenario>();

        private IEventService _eventService; // Currently not strictly used directly but kept for potential future use or consistency if needed

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
        }

        private void Start()
        {
            // Subscribe to PlayerMissEvent
            MessageBroker.Default.Receive<PlayerMissEvent>()
                .Subscribe(_ => HandleMissEvent())
                .AddTo(this);

            // Subscribe to PlayerDeathEvent
            MessageBroker.Default.Receive<PlayerDeathEvent>()
                .Subscribe(_ => OnDeath?.Invoke())
                .AddTo(this);
        }

        private void HandleMissEvent()
        {
            bool blocked = false;

            foreach (var scenario in _specialMissScenarios)
            {
                if (CheckScenarioConditions(scenario))
                {
                    scenario.OnMiss?.Invoke();
                    if (scenario.BlockStandardMiss)
                    {
                        blocked = true;
                    }
                }
            }

            if (!blocked)
            {
                OnMiss?.Invoke();
            }
        }

        private bool CheckScenarioConditions(SpecialMissScenario scenario)
        {
            if (scenario.Conditions == null || scenario.Conditions.Length == 0)
            {
                // If no conditions are set, treat it as always true (or false? usually conditions imply restriction)
                // Let's assume if added to the list, it's intended to check something. 
                // But if empty, it's safer to say "no conditions met" or "always met"? 
                // Given the context of "Special", usually requires a condition. 
                // However, matching logic from EventTriggerBase:
                // if (_conditions.Count == 0) return true;
                return true;
            }

            foreach (var component in scenario.Conditions)
            {
                if (component != null)
                {
                    var condition = component.CreateCondition();
                    if (!condition.IsSatisfied())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
