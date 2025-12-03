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
            Debug.Log($"[EnemyMoveHandler] StartChase called. Time: {Time.time}");
            // EnemySpawnManager.Instance.SetupToStoryMode();

            var enemy = EnemySpawnManager.Instance.CurrentEnemyInstance;
            if (enemy != null)
            {
                Debug.Log($"[EnemyMoveHandler] CurrentEnemyInstance found: {enemy.name}");
                var eventLock = enemy.GetComponent<EventLockControl>();
                if (eventLock != null)
                {
                    Debug.Log($"[EnemyMoveHandler] EventLockControl found. Calling EndLock. IsActive before: {eventLock.IsActive}");
                    eventLock.EndLock();
                    Debug.Log($"[EnemyMoveHandler] EventLockControl IsActive after: {eventLock.IsActive}");
                }
                else
                {
                    Debug.LogWarning("[EnemyMoveHandler] EventLockControl NOT found on enemy.");
                }

                var ai = enemy.GetComponent<EnemyAIController>();
                if (ai != null)
                {
                    Debug.Log($"[EnemyMoveHandler] EnemyAIController found. Setting Chasing=true. IsChasing before: {ai.IsChasing}");
                    ai.SetChasing(true);
                    ai.SetStoryMode(false);
                    Debug.Log($"[EnemyMoveHandler] EnemyAIController IsChasing after: {ai.IsChasing}");
                }
                else
                {
                    Debug.LogWarning("[EnemyMoveHandler] EnemyAIController NOT found on enemy.");
                }
            }
            else
            {
                Debug.LogWarning("[EnemyMoveHandler] CurrentEnemyInstance is NULL.");
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
