using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerMoveHandler : MonoBehaviour
    {   
        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            ForcedMovementController playerForcedTransformMover = FindAnyObjectByType<ForcedMovementController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.ForceMoveTo(target.transform.position);
        }

        public void MoveToTarget(GameObject target)
        {
            MoveToTarget(target, false);
        }

        public void MoveToTargetBackstep(GameObject target)
        {
            MoveToTarget(target, true);
        }
    }
}