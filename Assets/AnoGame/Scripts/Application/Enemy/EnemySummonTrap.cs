using UnityEngine;
using UnityEngine.AI;
using AnoGame.Application.Direction;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// 特定のイベント（缶を蹴るなど）発生時に、キラーを強制的に特定の場所に呼び寄せるトラップ
    /// </summary>
    public class EnemySummonTrap : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("呼び出す際にChaseモードに強制移行するか")]
        [SerializeField] private bool forceChase = true;

        [Tooltip("テレポート後の硬直時間（秒）")]
        [SerializeField] private float freezeDuration = 1.0f;

        [Header("Random Summon Settings")]
        [SerializeField] private Transform[] summonPoints;

        /// <summary>
        /// 登録された summonPoints からランダムな位置を選んでキラーを召喚する
        /// </summary>
        public void SummonRandom()
        {
            if (summonPoints == null || summonPoints.Length == 0)
            {
                Debug.LogWarning("[EnemySummonTrap] SummonPoints is empty.");
                return;
            }

            int index = Random.Range(0, summonPoints.Length);
            Summon(summonPoints[index]);
        }

        /// <summary>
        /// 指定したターゲット位置へキラーをテレポートさせ、発見状態にする
        /// </summary>
        /// <param name="target">テレポート先のTransform</param>
        public void Summon(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[EnemySummonTrap] Target transform is null.");
                return;
            }

            GameObject enemy = GetCurrentEnemy();
            if (enemy == null)
            {
                Debug.LogWarning("[EnemySummonTrap] Enemy not found.");
                return;
            }

            // 4. Set State (Moved up for early check preference, but logic wise: Check existing state first)
            var coordinator = enemy.GetComponent<EnemyBehaviorCoordinator>();

            // [NEW] Chase中ならワープしない
            if (coordinator != null && coordinator.IsChasing)
            {
                Debug.Log("[EnemySummonTrap] Summon skipped because enemy is already Chasing.");
                return;
            }

            // 1. Warp
            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(target.position);
                // 向きも合わせる
                enemy.transform.rotation = target.rotation;
            }
            else
            {
                // NavMeshAgentがない場合（稀）
                enemy.transform.position = target.position;
                enemy.transform.rotation = target.rotation;
            }

            Debug.Log($"[EnemySummonTrap] Summoned Enemy to {target.name}");

            // 2. Reaction check (もし既存のReactionがあればキャンセルなど)
            var reaction = enemy.GetComponent<EnemyTeleportReaction>();
            if (reaction != null)
            {
                reaction.CancelDisableTimer();
            }

            // 3. Freeze (Optional)
            // いきなり動くと不自然な場合があるので少し止める処理などを挟む余地

            // 4. Force Chase
            if (forceChase)
            {
                if (coordinator != null)
                {
                    coordinator.NotifyFound();
                }
            }
        }

        private GameObject GetCurrentEnemy()
        {
            if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.CurrentEnemyInstance != null)
            {
                return EnemySpawnManager.Instance.CurrentEnemyInstance;
            }
            // Add other lookup logic if necessary
            return GameObject.FindGameObjectWithTag("Enemy");
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Summon to Here")]
        private void DebugSummon()
        {
            Summon(this.transform);
        }
#endif
    }
}
