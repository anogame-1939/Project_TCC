namespace AnoGame.Domain.Event.Conditions
{
    public interface IEventCondition
    {
        bool IsSatisfied();
    }
}