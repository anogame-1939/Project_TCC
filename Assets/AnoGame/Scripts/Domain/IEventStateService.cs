using System.Collections.Generic;

namespace AnoGame.Domain.Event.Services
{
    public interface IEventService 
    {
        void TriggerKeyItemEvent(string itemName);
        void RegisterKeyItemHandler(string itemName, System.Action handler);
        void UnregisterKeyItemHandler(string itemName, System.Action handler);
    }
    public interface IEventStateService
    {
        bool IsEventCleared(string eventId);
        void SetEventCleared(string eventId);
        void RestoreEventStates(IEnumerable<string> clearedEvents);
    }
}
