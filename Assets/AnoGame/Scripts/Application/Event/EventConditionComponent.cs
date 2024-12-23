using UnityEngine;
using AnoGame.Domain.Event.Conditions;
public abstract class EventConditionComponent : MonoBehaviour
{
    public abstract IEventCondition CreateCondition();
}