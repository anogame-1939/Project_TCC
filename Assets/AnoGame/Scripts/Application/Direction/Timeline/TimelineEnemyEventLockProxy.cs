using UnityEngine;
using AnoGame.Application.Player.Control; // EventLockControl
using AnoGame.Application.Enemy; // EnemySpawnManager

namespace AnoGame.Application.Direction.Timeline
{
    [DisallowMultipleComponent]
    public sealed class TimelineEnemyEventLockProxy : MonoBehaviour
    {
        [SerializeField] private EnemySpawnManager _spawnManager;

        private EventLockControl _enemyCtrl
        {
            get
            {
                if (_spawnManager == null) return null;
                if (_spawnManager.CurrentEnemyInstance == null) return null;
                return _spawnManager.CurrentEnemyInstance.GetComponent<EventLockControl>();
            }
        }

        // タイムラインから叩きたい薄いAPI
        public void BeginLock() => _enemyCtrl?.BeginLock();
        public void EndLock() => _enemyCtrl?.EndLock();

        public void MoveTo(Transform target, float speed, float stopDistance)
        {
            if (_enemyCtrl == null || target == null) return;
            _enemyCtrl.MoveToPoint(target.position, speed, stopDistance);
        }

        public void LookKeep() => _enemyCtrl?.LookKeep();
        public void LookFaceMove() => _enemyCtrl?.LookFaceMove();
        public void LookAt(Transform t) => _enemyCtrl?.LookAt(t);
    }
}
