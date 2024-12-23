using UnityEngine;
using UnityEngine.Events;
using VContainer;
using AnoGame.Domain.Event;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Event.Types;
using AnoGame.Data;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Domain.Event.Conditions;

namespace AnoGame.Application.Event
{
    public abstract class EventTriggerBase : MonoBehaviour
    {
        [SerializeField] protected EventData eventData;
        private IEventSettings EventSettings => eventData;
        [SerializeField] protected UnityEvent onEventStart;
        [SerializeField] protected UnityEvent onEventComplete;
        [SerializeField] protected EventConditionComponent[] conditionComponents;

        [Inject] protected IEventProgressService _eventProgressService;
        
        private List<IEventCondition> _conditions = new List<IEventCondition>();

        [Inject]
        public virtual void Construct(IEventProgressService eventProgressService)
        {
            _eventProgressService = eventProgressService;
        }

        protected virtual void Start()
        {
            InitializeConditions();
        }

        protected virtual void InitializeConditions()
        {
            if (conditionComponents == null) return;
            
            foreach (var component in conditionComponents)
            {
                if (component != null)
                {
                    _conditions.Add(component.CreateCondition());
                }
            }
        }

        protected bool CheckConditions()
        {
            if (_conditions.Count == 0)
                return true;

            return _conditions.All(condition => condition.IsSatisfied());
        }

        protected virtual void TryTriggerEvent()
        {
            if (!_eventProgressService.CanTriggerEvent(eventData.EventId))
                return;

            if (!CheckConditions())
                return;

            _eventProgressService.StartEvent(eventData.EventId);
            onEventStart?.Invoke();
        }

        public virtual void CompleteEvent()
        {
            if (_eventProgressService.GetEventState(eventData.EventId) != EventState.InProgress)
                return;

            _eventProgressService.CompleteEvent(eventData.EventId);
            onEventComplete?.Invoke();

            if (!eventData.IsOneTime)
            {
                _eventProgressService.ResetEvent(eventData.EventId);
            }
        }
    }
}