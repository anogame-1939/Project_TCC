using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Player.Interaction
{
    public struct HideRequested
    {
        public Transform Actor;
        public HideSpotZone Spot;
        public HideRequested(Transform actor, HideSpotZone spot) { Actor = actor; Spot = spot; }
    }

    public class HideSpotZone : InteractableZone
    {
        [SerializeField] private string prompt = "隠れる";
        [SerializeField] private int priority = 900;

        // 実際は EventLockControl 等に接続
        public System.Action<Transform> EnterHide;

        public override bool TryBuildOptions(Transform actor, List<InteractionOption> buffer)
        {
            Debug.Log($"[Interaction] HideRequested TryBuildOptions: {actor.name} -> {name}", this);
            Debug.Log($"  InDistance: {InDistance(actor)}, InAngle: {InAngle(actor)}, HasLoS: {HasLoS(actor)}", this);
            if (!InDistance(actor) || !InAngle(actor) || !HasLoS(actor)) return false;

            buffer.Add(new InteractionOption
            {
                Kind = InteractionKind.Hide,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = false,
                Execute = () =>
                {
                    Debug.Log($"[Interaction] HideRequested: {actor.name} -> {name}", this);
                    EnterHide?.Invoke(actor);
                    UniRx.MessageBroker.Default.Publish(new HideRequested(actor, this));
                }
            });
            return true;
        }
    }
}
