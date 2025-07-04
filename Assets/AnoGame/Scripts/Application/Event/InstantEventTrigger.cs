using UnityEngine;
using VContainer;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// イベントがスタートしたら即時にクリアする
    /// </summary>
    [DefaultExecutionOrder(999)] 
    public class InstantEventTrigger : EventTriggerBase
    {
        [SerializeField]
        private bool _onStart = false;
        [Inject] private EventManager _eventManager;

        [Inject]
        public void Construct(
            EventManager eventManager
        )
        {
            _eventManager = eventManager;
        }

        protected override void Start()
        {
            if (_onStart)
            {
                base.Start();
                base._eventService.TriggerEventStart(eventData.EventId);
            }
        }

        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            Debug.Log($"InstantEventTrigger-OnStartEvent:{name}");

            // スタートと同時にイベントをクリアする
            _eventService.TriggerEventComplete(eventData.EventId);
            _eventManager.AddClearedEvent(eventData.EventId);

        }

        public override void OnFinishEvent()
        {
            base.OnFinishEvent();
            Debug.Log($"InstantEventTrigger-OnCompleteEvent:{name}");
        }
    }
}