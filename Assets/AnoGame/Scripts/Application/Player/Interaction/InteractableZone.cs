using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Player.Interaction
{
    [RequireComponent(typeof(BoxCollider))]
    public abstract class InteractableZone : MonoBehaviour, IInteractable
    {
        [Header("Gate")]
        [SerializeField] protected float maxDistance = 2.0f;
        [SerializeField] protected float viewAngle = 120f;
        [SerializeField] protected LayerMask losMask = ~0;
        [SerializeField] protected Transform uiAnchor;

        public virtual Vector3 WorldUiAnchor
            => uiAnchor ? uiAnchor.position : transform.position + Vector3.up * 1.5f;

        protected bool InAngle(Transform actor)
        {
            var dir = (transform.position - actor.position).normalized;
            var ang = Vector3.Angle(actor.forward, dir);
            return ang <= viewAngle * 0.5f;
        }

        protected bool InDistance(Transform actor)
            => Vector3.Distance(actor.position, transform.position) <= maxDistance;

        protected bool HasLoS(Transform actor)
        {
            var origin = actor.position + Vector3.up * 1.6f;
            var target = WorldUiAnchor;
            if (Physics.Linecast(origin, target, out var hit, losMask))
                return hit.transform == transform;
            return true; // マスク未設定なら通す
        }

        public virtual float Score(Transform actor)
        {
            if (!InDistance(actor) || !InAngle(actor) || !HasLoS(actor)) return float.NegativeInfinity;
            var d = Vector3.Distance(actor.position, transform.position);
            return 1000f - d * 100f; // 近いほど高スコア（簡易）
        }

        public abstract bool TryBuildOptions(Transform actor, List<InteractionOption> buffer);
    }
}
