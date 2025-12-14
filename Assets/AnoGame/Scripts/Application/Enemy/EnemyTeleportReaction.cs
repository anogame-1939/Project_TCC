using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using UniRx;
using AnoGame.Domain.Event;
using AnoGame.Application.Enemy.AI;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// プレイヤーのライフ減少時に、敵をSpline上のランダムな位置にワープさせる
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyTeleportReaction : MonoBehaviour
    {
        [SerializeField] private PatrolSplineIntentProvider _patrolProvider;

        // 参照取得用
        private NavMeshAgent _agent;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_patrolProvider == null)
            {
                _patrolProvider = GetComponentInChildren<PatrolSplineIntentProvider>();
            }

            // イベント購読
            MessageBroker.Default.Receive<PlayerMissEvent>()
                .Subscribe(OnPlayerLifeLost)
                .AddTo(this);
        }

        private void OnPlayerLifeLost(PlayerMissEvent e)
        {
            if (_agent == null || _patrolProvider == null) return;

            // プレイヤー（現在地）から最も遠い位置へワープ
            WarpToFarthestSplinePoint();
        }

        private void WarpToFarthestSplinePoint()
        {
            var container = GetSplineContainer();
            if (container == null) return;

            Vector3 currentPos = transform.position;
            Vector3 bestPos = currentPos;
            float maxDistSq = -1f;

            // 5%刻みでサンプリングして、最も遠い地点を探す
            // 精査が必要なら刻みを小さくする（例: 0.02f）
            for (float t = 0; t <= 1.0f; t += 0.05f)
            {
                Vector3 worldPos = container.EvaluatePosition(t);

                // ヒットした時点での自分の位置からの距離
                float distSq = Vector3.SqrMagnitude(worldPos - currentPos);
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    bestPos = worldPos;
                }
            }

            // NavMesh上の近い位置を探す
            if (NavMesh.SamplePosition(bestPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);

                // Agentを一時的に無効化
                _agent.enabled = false;
                Debug.Log($"[EnemyTeleportReaction] Warped to Farthest: {hit.position} (Dist: {Mathf.Sqrt(maxDistSq)}). Agent disabled for 15s.");

                // 15秒後に有効化
                Observable.Timer(System.TimeSpan.FromSeconds(15))
                    .Subscribe(_ =>
                    {
                        if (_agent != null) _agent.enabled = true;
                        Debug.Log("[EnemyTeleportReaction] Agent re-enabled.");
                    })
                    .AddTo(this);
            }
        }

        private SplineContainer GetSplineContainer()
        {
            if (_patrolProvider != null)
            {
                return _patrolProvider.Container;
            }
            return null;
        }
        [Button]
        public void WarpToNearPlayerSplinePoint()
        {
            var container = GetSplineContainer();
            if (container == null)
            {
                Debug.LogWarning("SplineContainer is null");
                return;
            }

            // プレイヤーを探す
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var stealth = FindFirstObjectByType<AnoGame.Application.Player.Controller.PlayerStealthController>();
                if (stealth != null) player = stealth.gameObject;
            }

            if (player == null)
            {
                Debug.LogWarning("Player not found");
                return;
            }

            Vector3 targetPos = player.transform.position;
            Vector3 bestPos = targetPos;
            float minDistSq = float.MaxValue;

            // 調整用パラメータ
            float samplingStep = 0.05f;

            for (float t = 0; t <= 1.0f; t += samplingStep)
            {
                Vector3 worldPos = container.EvaluatePosition(t);

                float distSq = Vector3.SqrMagnitude(worldPos - targetPos);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    bestPos = worldPos;
                }
            }

            if (_agent == null) _agent = GetComponent<NavMeshAgent>();

            if (NavMesh.SamplePosition(bestPos, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                Debug.Log($"[EnemyTeleportReaction] Debug Warped to Near Player: {hit.position} (Dist: {Mathf.Sqrt(minDistSq)})");
            }
            else
            {
                Debug.LogWarning("Failed to find valid NavMesh position near spline point.");
            }
        }
    }
}
