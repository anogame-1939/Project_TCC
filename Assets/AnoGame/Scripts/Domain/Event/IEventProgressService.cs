namespace AnoGame.Domain.Event.Services
{
    public interface IEventProgressService
    {
        EventState GetEventState(string eventId);
        void StartEvent(string eventId);
        void CompleteEvent(string eventId);
        void ResetEvent(string eventId);
        bool CanTriggerEvent(string eventId);
    }
}