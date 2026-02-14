using UnityEngine;
using AnoGame.Domain.Event.Conditions;

namespace AnoGame.AnoFlow
{
    public abstract class EventConditionComponent : MonoBehaviour
    {
        public abstract IEventCondition CreateCondition();
    }
}