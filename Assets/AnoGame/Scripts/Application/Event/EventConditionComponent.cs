using UnityEngine;
using AnoGame.Domain.Event.Conditions;

namespace AnoGame.Application.Event
{
    public abstract class EventConditionComponent : MonoBehaviour
    {
        public abstract IEventCondition CreateCondition();
    }
}