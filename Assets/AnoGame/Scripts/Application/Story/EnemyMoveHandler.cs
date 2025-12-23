using AnoGame.Application.Enemy;
using AnoGame.Application.Enemy.AI;
using AnoGame.Application.Player.Control;
using AnoGame.Application.Direction;
using UnityEngine;
using AnoGame.Application.Audio;

namespace AnoGame.Application.Story
{
    public class EnemyMoveHandler : MonoBehaviour
    {
        public void StartChase()
        {
            Debug.Log($"[EnemyMoveHandler] StartChase called. Time: {Time.time}");

            var enemy = EnemySpawnManager.Instance.CurrentEnemyInstance;
            if (enemy != null)
            {
                Debug.Log($"[EnemyMoveHandler] CurrentEnemyInstance found: {enemy.name}");
                // 1. EventLock (Existing)
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

                // 2. New System: Enable Chase, Disable Patrol
                var chaseIntent = enemy.GetComponent<ChaseIntentProvider>();
                if (chaseIntent != null)
                {
                    Debug.Log("[EnemyMoveHandler] Enabling ChaseIntentProvider.");
                    chaseIntent.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[EnemyMoveHandler] ChaseIntentProvider NOT found on enemy.");
                }

                var patrolIntent = enemy.GetComponent<PatrolSplineIntentProvider>();
                if (patrolIntent != null)
                {
                    Debug.Log("[EnemyMoveHandler] Disabling PatrolSplineIntentProvider.");
                    patrolIntent.SetActive(false);
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

        // [NEW] 外部入力を一時的にブロックする（Timeline等から呼び出し用）
        public void SetEnemyInputBlocked(bool blocked)
        {
            EventLockControl ctrl = FindActiveEnemyEventLock();
            if (ctrl == null) return;

            var coordinator = ctrl.GetComponent<EnemyBehaviorCoordinator>();
            if (coordinator != null)
            {
                Debug.Log($"[EnemyMoveHandler] SetEnemyInputBlocked({blocked}) called.");
                coordinator.SetInputBlocked(blocked);
            }
        }

        public void StopHeartbeat()
        {
            var heartbeatController = FindAnyObjectByType<HeartbeatController>();
            if (heartbeatController != null)
            {
                heartbeatController.StopImmediate();
            }
        }
    }
}
