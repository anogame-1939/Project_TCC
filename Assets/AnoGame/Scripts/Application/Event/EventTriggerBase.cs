using UnityEngine;
using UnityEngine.Events;
using VContainer;
using AnoGame.Domain.Event;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Event.Types;
using AnoGame.Data;
using System.Collections.Generic;
using AnoGame.Domain.Event.Conditions;

namespace AnoGame.Application.Interaction.Components
{
    public abstract class EventTriggerBase : MonoBehaviour
    {
        [SerializeField] protected EventData eventData;
        private IEventSettings EventSettings => eventData;
        [SerializeField] protected UnityEvent onEventStart;
        [SerializeField] protected UnityEvent onEventComplete;

        [Inject] protected IEventProgressService _eventProgressService;
        
        protected List<IEventCondition> _conditions = new List<IEventCondition>();

        [Inject]
        public virtual void Construct(IEventProgressService eventProgressService)
        {
            _eventProgressService = eventProgressService;
        }

        protected virtual void Start()
        {
            InitializeConditions();
        }

        protected abstract void InitializeConditions();

        protected bool CheckConditions()
        {
            return _conditions.TrueForAll(condition => condition.IsSatisfied());
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