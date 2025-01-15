using System;
using System.Collections.Generic;

namespace AnoGame.Domain.Event.Services
{
    public interface IEventService
    {
        event Action LoadedClearEvent;
        void SetCleadEvents(HashSet<string> clearedEventIDs);
        bool IsEventCleared(string eventID);
        void RegisterStartEventHandler(string eventID, System.Action handler);
        void RegisterCompleteEventHandler(string eventID, System.Action handler);
        void RegisterFailedEventHandler(string eventID, System.Action handler);
        void UnregisterStartEventHandler(string eventID, System.Action handler);
        void UnregisterCompleteEventHandler(string eventID, System.Action handler);
        void UnregisterFailedEventHandler(string eventID, System.Action handler);
        void TriggerEventStart(string eventID);
        void TriggerEventComplete(string eventID);
        void TriggerEventFailed(string eventID);
    }
}
