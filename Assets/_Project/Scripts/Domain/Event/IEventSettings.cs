namespace AnoGame.Domain.Event.Types
{
    public interface IEventSettings
    {
        string EventId { get; }
        string EventName { get; }
        bool IsOneTime { get; }
    }
}