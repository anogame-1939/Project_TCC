using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AnoGame.Application.Player.Interaction
{
    public class InspectZone : InteractableZone
    {
        [SerializeField] private string prompt = "調べる（長押し）";
        [SerializeField] private int priority = 500;

        public UnityEvent OnInspected; // Timeline再生/ログ追加など

        public override bool TryBuildOptions(Transform actor, List<InteractionOption> buffer)
        {
            if (!InDistance(actor) || !InAngle(actor) || !HasLoS(actor)) return false;

            buffer.Add(new InteractionOption{
                Kind = InteractionKind.Inspect,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = true,
                Execute = () => OnInspected?.Invoke()
            });
            return true;
        }
    }
}
