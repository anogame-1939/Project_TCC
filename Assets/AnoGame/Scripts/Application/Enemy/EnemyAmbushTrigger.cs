using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AnoGame.Application.Direction;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// プレイヤーの通行を監視し、指定回数通過後にキラーを待ち伏せさせるトリガー
    /// </summary>
    public class EnemyAmbushTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("最低通過回数")]
        [SerializeField] private int minPassCount = 2;
        [Tooltip("最大通過回数")]
        [SerializeField] private int maxPassCount = 5;

        [Tooltip("待ち伏せ出現候補地点リスト")]
        [SerializeField] private List<Transform> ambushPoints = new List<Transform>();

        [Header("Debug")]
        [SerializeField] private int _currentLimit;
        [SerializeField] private int _currentPassCount;
        [SerializeField] private bool _isTriggered;

        private void Start()
        {
            ResetTrigger();
        }

        private void ResetTrigger()
        {
            _currentLimit = Random.Range(minPassCount, maxPassCount + 1);
            _currentPassCount = 0;
            _isTriggered = false;
            Debug.Log($"[EnemyAmbushTrigger] Initialized. Limit: {_currentLimit}");
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isTriggered) return;

            if (other.CompareTag("Player"))
            {
                _currentPassCount++;
                Debug.Log($"[EnemyAmbushTrigger] Player Pass: {_currentPassCount}/{_currentLimit}");

                if (_currentPassCount >= _currentLimit)
                {
                    ExecuteAmbush();
                }
            }
        }

        private void ExecuteAmbush()
        {
            _isTriggered = true;
            Debug.Log("[EnemyAmbushTrigger] Ambush Triggered!");

            if (ambushPoints == null || ambushPoints.Count == 0)
            {
                Debug.LogWarning("[EnemyAmbushTrigger] No ambush points defined.");
                return;
            }

            // 出現地点をランダム決定
            Transform targetPoint = ambushPoints[Random.Range(0, ambushPoints.Count)];

            // キラーを召喚（EnemySummonTrapのロジックを流用しても良いが、ここでは直接書くか、共通化する）
            // せっかく作ったのでEnemySummonTrap的なロジックを内包する
            SummonEnemy(targetPoint);
        }

        private void SummonEnemy(Transform target)
        {
            GameObject enemy = GetCurrentEnemy();
            if (enemy == null)
            {
                Debug.LogWarning("[EnemyAmbushTrigger] Enemy not found for ambush.");
                return;
            }

            // Chaseモードへ
            var coordinator = enemy.GetComponent<EnemyBehaviorCoordinator>();
            if (coordinator != null)
            {
                // [NEW] 既にChase中ならワープせず終了
                if (coordinator.IsChasing)
                {
                    Debug.Log("[EnemyAmbushTrigger] Ambush skipped because enemy is already Chasing.");
                    return;
                }
            }

            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(target.position);
                enemy.transform.rotation = target.rotation;
            }
            else
            {
                enemy.transform.position = target.position;
                enemy.transform.rotation = target.rotation;
            }

            if (coordinator != null)
            {
                coordinator.NotifyFound();
            }

            // Reactionがあればキャンセル
            var reaction = enemy.GetComponent<EnemyTeleportReaction>();
            if (reaction != null)
            {
                reaction.CancelDisableTimer();
            }
        }

        private GameObject GetCurrentEnemy()
        {
            if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.CurrentEnemyInstance != null)
            {
                return EnemySpawnManager.Instance.CurrentEnemyInstance;
            }
            return GameObject.FindGameObjectWithTag("Enemy");
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Trigger Ambush")]
        public void DebugTriggerAmbush()
        {
            ExecuteAmbush();
        }

        [ContextMenu("Reset Counter")]
        public void DebugReset()
        {
            ResetTrigger();
        }
#endif
    }
}
