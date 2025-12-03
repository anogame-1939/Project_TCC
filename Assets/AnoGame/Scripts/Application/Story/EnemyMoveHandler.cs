using AnoGame.Application.Enemy;
using AnoGame.Application.Enmemy.Control;
using AnoGame.Application.Player.Control;
using UnityEngine;

namespace AnoGame.Application.Story
{
    public class EnemyMoveHandler : MonoBehaviour
    {
        public void StartChase()
        {
            // EnemySpawnManager.Instance.SetupToStoryMode();

            var enemy = EnemySpawnManager.Instance.CurrentEnemyInstance;
            if (enemy != null)
            {
                var eventLock = enemy.GetComponent<EventLockControl>();
                if (eventLock != null)
                {
                    eventLock.EndLock();
                }

                var ai = enemy.GetComponent<EnemyAIController>();
                if (ai != null)
                {
                    ai.SetChasing(true);
                    ai.SetStoryMode(false);
                }
            }
        }

        // ---------------------------------------------------------
        // EventLockControlを探す
        // ---------------------------------------------------------
        private EventLockControl FindActiveEnemyEventLock()
        {
            var enemy = EnemySpawnManager.Instance.CurrentEnemyInstance;
            if (enemy != null && enemy.activeInHierarchy)
            {
                return enemy.GetComponent<EventLockControl>();
            }

            // Fallback
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var e in enemies)
            {
                if (e.activeInHierarchy)
                {
                    var ctrl = e.GetComponent<EventLockControl>();
                    if (ctrl != null && ctrl.isActiveAndEnabled)
                    {
                        return ctrl;
                    }
                }
            }
            return null;
        }

        public void DisableForceMode()
        {
            StartChase();
        }

        private void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            EventLockControl ctrl = FindActiveEnemyEventLock();
            if (ctrl == null || target == null) return;

            ctrl.BeginLock();

            if (doBackstep)
            {
                // Backstep: Move to target but face opposite to movement (Moonwalk)
                ctrl.MoveToPoint(target.transform.position, 5f);
                ctrl.LookFaceOppositeMove();
            }
            else
            {
                ctrl.MoveToPoint(target.transform.position, 5f);
                ctrl.LookFaceMove();
            }
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
            EventLockControl ctrl = FindActiveEnemyEventLock();
            if (ctrl == null) return;

            ctrl.BeginLock();
            ctrl.TurnToAngle(angle);
        }

        public void FaceToTarget(GameObject target)
        {
            // PlayerActionController logic remains for Player
            PlayerActionController playerForcedTransformMover = FindAnyObjectByType<PlayerActionController>();
            if (playerForcedTransformMover == null) return;

            playerForcedTransformMover.FaceTarget(target);
        }
    }
}
