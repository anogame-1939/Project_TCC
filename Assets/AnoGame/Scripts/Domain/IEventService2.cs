
namespace AnoGame.Domain.Event.Services
{
    public interface IEventService2
    {
        void RegisterStartEventHandler(string eventID, System.Action handler);
        void RegisterCompleteEventHandler(string eventID, System.Action handler);
        void UnregisterStartEventHandler(string eventID, System.Action handler);
        void UnregisterCompleteEventHandler(string eventID, System.Action handler);
    }
}
