using System.Collections.Generic;
using UnityEngine;
using AnoGame.Domain.Event.Conditions;
using AnoGame.Data;
using AnoGame.Domain.Event.Services;
using VContainer;

namespace AnoGame.AnoFlow
{
    public class SimpleEventConditionComponent : EventConditionComponent
    {
        private IEventService _eventService;
        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
        }

        // 複数のEventDataをインスペクターで設定できるようにする
        [SerializeField] private List<EventData> _eventDataList;

        // クリアしていては *いけない* イベントリスト（これらの一つでもクリア済みなら条件不成立）
        [SerializeField] private List<EventData> _mustNotBeClearedEventDataList;

        public override IEventCondition CreateCondition()
        {
            // EventDataリストからeventIdのリストに変換
            var eventIdList = _eventDataList != null ? _eventDataList.ConvertAll(e => e != null ? e.EventId : "") : new List<string>();
            var excludedEventIdList = _mustNotBeClearedEventDataList != null ? _mustNotBeClearedEventDataList.ConvertAll(e => e != null ? e.EventId : "") : new List<string>();

            return new MultipleEventCondition(_eventService, eventIdList, excludedEventIdList);
        }
    }
}
