using UnityEngine;
using VContainer;
using AnoGame.Data;
using AnoGame.Application.Attributes;
using System.Reflection;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// イベントがスタートしたら即時にクリアする
    /// (拡張: IDを文字列指定してランタイムでEventDataを生成して動作)
    /// </summary>
    [DefaultExecutionOrder(999)]
    public class InstantEventTrigger : EventTriggerBase
    {
        [Header("Override")]
        [EventSelector]
        [SerializeField] private string targetEventId;

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
            // EventDataが未設定で、targetEventIdがある場合、ランタイム生成で補完
            if (eventData == null && !string.IsNullOrEmpty(targetEventId))
            {
                eventData = ScriptableObject.CreateInstance<EventData>();
                // private field 'eventId' にアクセスしてセット
                var field = typeof(EventData).GetField("eventId", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) field.SetValue(eventData, targetEventId);
            }

            base.Start();
            if (_onStart && eventData != null)
            {
                // イベント開始をトリガーする
                base._eventService.TriggerEventStart(eventData.EventId);
            }
        }

        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            // Debug.Log($"InstantEventTrigger-OnStartEvent:{name}", this);

            if (CheckConditions())
            {
                if (eventData == null) return;

                // スタートと同時にイベントをクリアする
                _eventService.TriggerEventComplete(eventData.EventId);
                _eventManager.AddClearedEvent(eventData.EventId);
            }

        }

        public override void OnFinishEvent()
        {
            base.OnFinishEvent();
            // Debug.Log($"InstantEventTrigger-OnCompleteEvent:{name}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspector表示調整: BaseのeventDataは見えなくしたいが、
            // Editor拡張を書かないと完全には消せない。
            // ここでは targetEventId があればそれを優先するロジックのみ担保
        }
#endif
    }
}