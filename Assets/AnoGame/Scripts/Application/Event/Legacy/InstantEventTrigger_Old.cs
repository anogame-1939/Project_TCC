using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.AnoFlow;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.AnoFlow.Legacy
{
    /// <summary>
    /// イベントがスタートしたら即時にクリアする (Legacy Backup)
    /// </summary>
    [DefaultExecutionOrder(999)]
    public class InstantEventTrigger_Old : EventTriggerBase
    {
        [SerializeField]
        private bool _onStart = false;
        [Inject] private EventManager _eventManager;

        [Inject]
        public override void Construct(
            IEventService eventService,
             IInventoryService inventoryService
        )
        {
            base.Construct(eventService, inventoryService);
            // _eventManager = eventManager; // EventManager injection handled differently in base or here? 
            // Original code had Construct(EventManager).
            // But base has Construct(IEventService).
            // We need to keep signature unique or override?
            // "public void Construct(EventManager eventManager)" was the original.
            // But if it overrides base... Base is "public virtual void Construct(IEventService eventService)".
            // Original didn't have override keyword on Construct?
            // Step 185: [Inject] public void Construct(EventManager eventManager) { ... }
            // It did NOT override. It was overload.
        }

        [Inject]
        public void Construct(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        protected override void Start()
        {
            base.Start();
            if (_onStart)
            {
                // イベント開始をトリガーする
                base._eventService.TriggerEventStart(eventData.EventId);
            }
        }

        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            Debug.Log($"InstantEventTrigger-OnStartEvent:{name}", this);

            if (CheckConditions())
            {
                // スタートと同時にイベントをクリアする
                _eventService.TriggerEventComplete(eventData.EventId);
                _eventManager.AddClearedEvent(eventData.EventId);
            }

        }

        public override void OnFinishEvent()
        {
            base.OnFinishEvent();
            Debug.Log($"InstantEventTrigger-OnCompleteEvent:{name}", this);
        }
    }
}
