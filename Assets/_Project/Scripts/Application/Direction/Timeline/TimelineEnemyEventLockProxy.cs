using UnityEngine;
using AnoGame.Application.Player.Control; // EventLockControl
using AnoGame.Application.Enemy; // EnemySpawnManager
using VContainer;

namespace AnoGame.Application.Direction.Timeline
{
    [DisallowMultipleComponent]
    public sealed class TimelineEnemyEventLockProxy : MonoBehaviour
    {
        [Inject] private EnemySpawnManager _spawnManager;

        public EventLockControl EnemyCtrl
        {
            get
            {
                if (_spawnManager == null)
                {
                    _spawnManager = EnemySpawnManager.Instance;
                }
                if (_spawnManager.CurrentEnemyInstance == null) return null;
                return _spawnManager.CurrentEnemyInstance.GetComponent<EventLockControl>();
            }
        }

        void Start()
        {
            Debug.Log("_spawnManager: " + _spawnManager);
            Debug.Log($"[TimelineEnemyEventLockProxy] EnemyCtrl: {EnemyCtrl}");
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
