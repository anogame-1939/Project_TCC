using Unity.Plastic.Antlr3.Runtime;
using UnityEngine;

namespace AnoGame.Application.Event
{
    public class SimpleEventTrigger : EventTriggerBase
    {
        protected override void TryTriggerEvent()
        {
            base.TryTriggerEvent();
        }

        public override void CompleteEvent()
        {
            base.CompleteEvent();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                TryTriggerEvent();
            }
        }
    }
}