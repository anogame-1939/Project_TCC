using System;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Domain.Event.Services;

namespace AnoGame.Domain.Event.Conditions
{
    public class MultipleEventCondition : IEventCondition, IObservableCondition, IDisposable
    {
        private readonly IEventService _eventService;
        private readonly List<string> _requiredEventIDs;

        public event Action OnConditionChanged;

        public MultipleEventCondition(IEventService eventService, IEnumerable<string> requiredEventIDs)
        {
            _eventService = eventService;
            _requiredEventIDs = requiredEventIDs.ToList();
            // イベント状態がロード（または更新）されたときに条件変更を通知
            _eventService.LoadedClearEvent += HandleLoadedClearEvent;
        }

        private void HandleLoadedClearEvent()
        {
            // 状態変化を通知することで、条件が再評価されるようにする
            OnConditionChanged?.Invoke();
        }

        public bool IsSatisfied()
        {
            // 全ての指定されたイベントがクリアされているかチェック
            return _requiredEventIDs.All(eventID => _eventService.IsEventCleared(eventID));
        }

        public void Dispose()
        {
            _eventService.LoadedClearEvent -= HandleLoadedClearEvent;
        }
    }
}
