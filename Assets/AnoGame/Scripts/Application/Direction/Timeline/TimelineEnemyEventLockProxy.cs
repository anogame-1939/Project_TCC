using UnityEngine;
using AnoGame.Application.Player.Control; // EventLockControl
using AnoGame.Application.Enemy; // EnemySpawnManager

namespace AnoGame.Application.Direction.Timeline
{
    [DisallowMultipleComponent]
    public sealed class TimelineEnemyEventLockProxy : MonoBehaviour
    {
        [SerializeField] private EnemySpawnManager _spawnManager;

        public EventLockControl EnemyCtrl
        {
            get
            {
                if (_spawnManager == null) return null;
                if (_spawnManager.CurrentEnemyInstance == null) return null;
                return _spawnManager.CurrentEnemyInstance.GetComponent<EventLockControl>();
            }
        }

        // タイムラインから叩きたい薄いAPI
        public void BeginLock() => EnemyCtrl?.BeginLock();
        public void EndLock() => EnemyCtrl?.EndLock();

        public void MoveTo(Transform target, float speed, float stopDistance)
        {
            if (EnemyCtrl == null || target == null) return;
            EnemyCtrl.MoveToPoint(target.position, speed, stopDistance);
        }

        public void LookKeep() => EnemyCtrl?.LookKeep();
        public void LookFaceMove() => EnemyCtrl?.LookFaceMove();
        public void LookAt(Transform t) => EnemyCtrl?.LookAt(t);
    }
}
