using System;
using AnoGame.Domain.Event.Services;

namespace AnoGame.Domain.Event.Conditions
{
    public class EventClearedCondition : IEventCondition, IObservableCondition, IDisposable
    {
        private readonly IEventService _eventService;
        private readonly string _targetEventId;

        public event Action OnConditionChanged;

        public EventClearedCondition(IEventService eventService, string targetEventId)
        {
            _eventService = eventService;
            _targetEventId = targetEventId;
            // イベントクリアを監視する必要があるが、IEventServiceにイベントごとの監視APIがあるか確認が必要。
            // 現状のIEventService定義が見えないが、LoadedClearEventなどはある。
            // 個別のイベント完了通知があるか不明なため、全体通知をフックするか、要確認。
            // ここでは一旦、クリア済みかを判定するロジックのみ実装し、
            // 変更通知については IEventService.OncompleteEvent (仮) などを購読する想定で書く。
            // 既存コードの EventTriggerBase では _eventService.RegisterCompleteEventHandler がある。

            _eventService.RegisterCompleteEventHandler(_targetEventId, HandleEventCompleted);
        }

        public bool IsSatisfied()
        {
            return _eventService.IsEventCleared(_targetEventId);
        }

        private void HandleEventCompleted()
        {
            OnConditionChanged?.Invoke();
        }

        public void Dispose()
        {
            _eventService.UnregisterCompleteEventHandler(_targetEventId, HandleEventCompleted);
        }
    }
}
