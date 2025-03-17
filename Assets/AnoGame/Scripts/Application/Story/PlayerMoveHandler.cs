using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class PlayerMoveHandler : MonoBehaviour
    {   

        public void EnableForceMode()
        {
            ForcedMovementController playerForcedTransformMover = FindAnyObjectByType<ForcedMovementController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.EnableForceMode();
        }

        public void DisableForceMode()
        {
            ForcedMovementController playerForcedTransformMover = FindAnyObjectByType<ForcedMovementController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.DisableForceMode();
        }

        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            ForcedMovementController playerForcedTransformMover = FindAnyObjectByType<ForcedMovementController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.ForceMoveTo(target.transform.position, doBackstep);
        }

        public void MoveToTarget(GameObject target)
        {
            MoveToTarget(target, false);
        }

        public void MoveToTargetBackstep(GameObject target)
        {
            MoveToTarget(target, true);
        }

        public void SetAngle(float angle)
        {
            ForcedMovementController forcedMovementController = FindAnyObjectByType<ForcedMovementController>();
            if (forcedMovementController == null) return;

            forcedMovementController.SetAngle(angle);
        }

        public void FaceToTarget(GameObject target)
        {
            PlayerActionController playerForcedTransformMover = FindAnyObjectByType<PlayerActionController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.FaceTarget(target);
        }

    }
}