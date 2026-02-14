using UnityEngine;

namespace AnoGame.AnoFlow
{
    public class SimpleEventTrigger : EventTriggerBase
    {
        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            Debug.Log($"OnStartEvent-SimpleEventTrigger:{name}");
        }

        public override void OnFinishEvent()
        {
            base.OnFinishEvent();
            Debug.Log($"OnCompleteEvent-SimpleEventTrigger:{name}");
        }


    }
}