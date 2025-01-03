using System;
namespace AnoGame.Domain.Event.Conditions
{
    public interface IObservableCondition 
    {
        event Action OnConditionChanged;
    }
}