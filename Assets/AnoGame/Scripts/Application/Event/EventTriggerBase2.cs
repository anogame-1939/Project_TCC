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
    public abstract class EventTriggerBase2 : MonoBehaviour
    {
        [SerializeField] protected EventData eventData;
        public EventData EventData => eventData;
        private IEventSettings EventSettings => eventData;
        [SerializeField] protected UnityEvent onEventStart;
        [SerializeField] protected UnityEvent onEventComplete;
        [SerializeField] protected EventConditionComponent[] conditionComponents;

        private List<IEventCondition> _conditions = new List<IEventCondition>();

        [Inject] protected IEventService _eventService;

        [Inject]
        public virtual void Construct(IEventService eventService)
        {
            _eventService = eventService;
            _eventService.RegisterStartEventHandler(eventData.EventId, OnStartEvent);
            _eventService.RegisterCompleteEventHandler(eventData.EventId, OnCompleteEvent);
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

        public void StartEvent()
        {
            OnStartEvent();
        }

        protected virtual void OnStartEvent()
        {
            Debug.Log("OnStartEvent");
            // if (!CheckConditions())
                // return;

            onEventStart?.Invoke();
        }

        public virtual void OnCompleteEvent()
        {
            Debug.Log("OnCompleteEvent");
            // if (_eventProgressService.GetEventState(eventData.EventId) != EventState.InProgress)
                // return;

            onEventComplete?.Invoke();

            if (!eventData.IsOneTime)
            {
                // _eventProgressService.ResetEvent(eventData.EventId);
            }
        }
    }
}