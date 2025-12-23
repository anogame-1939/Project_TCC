using UnityEngine;
using UnityEngine.Events;
using AnoGame.Application.Enemy; // For EnemySpawnManager, EnemyBehaviorCoordinator
using AnoGame.Data;
using AnoGame.Application.Direction; // For SLFBRules if needed for TAG_PLAYER, or just literal string

namespace AnoGame.Application.Gimmicks
{
    /// <summary>
    /// 触れると一度だけ実行されるトラップ。
    /// 実行終了後、TriggerExitのタイミングで自身のSetActiveをfalseにする。
    /// Chase中かどうかで実行するイベントを振り分ける機能を持つ。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OneShotTriggerTrap : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Chase中かどうかにかかわらず常に実行される")]
        [SerializeField] private UnityEvent onTriggerEnterAlways;
        [SerializeField] private UnityEvent<Transform> onTriggerEnterAlwaysWithTransform;

        [Tooltip("Chase中でない時のみ実行される")]
        [SerializeField] private UnityEvent onTriggerEnterIfNotChasing;

        [Space]
        [Tooltip("実行後、プレイヤーがこの距離以上離れたら非表示にする")]
        [SerializeField] private float _disableDistance = 10.0f;

        private bool _isExecuted = false;
        private Transform _targetPlayer;

        private void OnTriggerEnter(Collider other)
        {
            if (_isExecuted) return;

            // Player判定
            if (other.CompareTag("Player"))
            {
                _targetPlayer = other.transform;
                ExecuteTrap();
                _isExecuted = true;
            }
        }

        private void Update()
        {
            if (!_isExecuted || _targetPlayer == null) return;

            // 距離判定で無効化
            float distSq = (_targetPlayer.position - transform.position).sqrMagnitude;
            if (distSq > _disableDistance * _disableDistance)
            {
                gameObject.SetActive(false);
            }
        }

        private void ExecuteTrap()
        {
            // 常に実行
            onTriggerEnterAlways?.Invoke();
            onTriggerEnterAlwaysWithTransform?.Invoke(_targetPlayer);

            // Chaseチェック
            if (!IsEnemyChasing())
            {
                onTriggerEnterIfNotChasing?.Invoke();
            }
        }

        private bool IsEnemyChasing()
        {
            // EnemySpawnManager から現在の Enemy を取得
            if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.CurrentEnemyInstance != null)
            {
                var enemy = EnemySpawnManager.Instance.CurrentEnemyInstance;
                var coordinator = enemy.GetComponent<EnemyBehaviorCoordinator>();
                if (coordinator != null)
                {
                    return coordinator.IsChasing;
                }
            }
            return false;
        }
    }
}
