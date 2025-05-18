using System.Collections.Generic;
using UnityEngine;
using AnoGame.Domain.Event.Conditions;
using AnoGame.Domain.Event.Services;
using VContainer;

namespace AnoGame.Application.Event
{
    public class MultipleEventConditionComponent : EventConditionComponent
    {
        [Inject] private IEventService _eventService;
        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
        }
        
        // 複数のイベントIDをインスペクターで設定できるようにする
        [SerializeField] private List<string> _eventIDs;

        public override IEventCondition CreateCondition()
        {
            return new MultipleEventCondition(_eventService, _eventIDs);
        }
    }
}
