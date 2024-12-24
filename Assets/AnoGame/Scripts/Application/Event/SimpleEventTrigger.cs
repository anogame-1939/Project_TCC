using Unity.Plastic.Antlr3.Runtime;
using UnityEngine;

namespace AnoGame.Application.Event
{
    public class SimpleEventTrigger : EventTriggerBase2
    {
        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            Debug.Log("OnStartEvent-SimpleEventTrigger");
        }

        public override void OnCompleteEvent()
        {
            base.OnCompleteEvent();
            Debug.Log("OnCompleteEvent-SimpleEventTrigger");
        }


    }
}