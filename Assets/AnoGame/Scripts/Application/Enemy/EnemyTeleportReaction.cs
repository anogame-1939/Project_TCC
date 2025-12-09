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
            MessageBroker.Default.Receive<PlayerLifeLostEvent>()
                .Subscribe(OnPlayerLifeLost)
                .AddTo(this);
        }

        private void OnPlayerLifeLost(PlayerLifeLostEvent e)
        {
            if (_agent == null || _patrolProvider == null) return;

            // 今回は、Warp処理を行う。ターゲット場所を決める。
            WarpToRandomSplinePoint();
        }

        private void WarpToRandomSplinePoint()
        {
            var container = GetSplineContainer();
            if (container == null) return;

            // ランダムな位置 (0.0 ~ 1.0)
            float t = Random.value;

            // Spline上の座標を取得 (World座標)
            Vector3 worldPos = container.EvaluatePosition(t);

            // NavMesh上の近い位置を探す (Splineが空中にあったりする場合の安全策)
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                Debug.Log($"[EnemyTeleportReaction] Warped to {hit.position}");
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
    }
}
