using System.Collections.Generic;

namespace AnoGame.Domain.Event.Services
{
    public class EventProgressService : IEventProgressService
    {
        private Dictionary<string, EventState> _eventStates = new Dictionary<string, EventState>();

        public EventState GetEventState(string eventId)
        {
            return _eventStates.TryGetValue(eventId, out var state) ? state : EventState.NotStarted;
        }

        public void StartEvent(string eventId)
        {
            _eventStates[eventId] = EventState.InProgress;
        }

        public void CompleteEvent(string eventId)
        {
            _eventStates[eventId] = EventState.Completed;
        }

        public void ResetEvent(string eventId)
        {
            _eventStates[eventId] = EventState.NotStarted;
        }

        public bool CanTriggerEvent(string eventId)
        {
            var state = GetEventState(eventId);
            return state == EventState.NotStarted;
        }
    }
}