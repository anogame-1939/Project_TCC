// ===================================
// Provider: 最寄り Knot へ帰投（最小）
//  - 到達したら自分で非アクティブ化して Patrol に明け渡す簡易版
// ===================================
using UnityEngine;
using UnityEngine.Splines;

namespace AnoGame.Application.Enemy.AI
{
    public class ReturnToAnchorIntentProvider : MonoBehaviour, IIntentProvider
    {
        [SerializeField] private int priority = 50;
        [SerializeField] private bool active = false;

        [Header("Spline/Anchor")]
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField, Min(0.05f)] private float arriveDistance = 1.5f; // 緩和
        [SerializeField] private float forceArriveDistance = 3.0f; // 詰まったとみなす距離

        private Vector3 _anchorWorld;
        private UnityEngine.AI.NavMeshAgent _agent;
        private float _stuckTimer = 0f;

        public int Priority => priority;

        public void Initialize(SplineContainer container)
        {
            splineContainer = container;
        }

        private void Start()
        {
            _agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
        }

        void Update()
        {
            // Debug.Log("Update:" + splineContainer);
        }

        public void ActivateToNearest()
        {
            active = true;
            _anchorWorld = FindNearestKnotWorld();
            _stuckTimer = 0f;
        }

        public void Deactivate() => active = false;

        public bool IsActive() => active && splineContainer != null && splineContainer.Spline != null;

        public bool TryGetGoal(out MoveGoal goal)
        {
            // Debug.Log("TryGetGoal:" + active);
            if (!active) { goal = default; return false; }
            // Debug.Log("TryGetGoal:" + active);

            // 到達したら自動で解除（巡回など下位に明け渡す）
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = _anchorWorld; b.y = 0f;
            float dist = Vector3.Distance(a, b);

            // Debug.Log("Distance:" + dist + " <= " + arriveDistance);

            bool arrived = false;

            // 1) 単純距離チェック
            if (dist <= arriveDistance)
            {
                arrived = true;
            }
            // 2) 詰まり対策：ある程度近付いているのに速度が出ない場合
            else if (_agent != null && dist <= forceArriveDistance)
            {
                // 停止に近い状態が続いたら到達とみなす
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
                {
                    // NavMeshAgent的には到着している
                    _stuckTimer += Time.deltaTime;
                }
                else if (_agent.velocity.sqrMagnitude < 0.01f)
                {
                    // 速度がほぼゼロ
                    _stuckTimer += Time.deltaTime;
                }
                else
                {
                    _stuckTimer = 0f;
                }

                if (_stuckTimer > 0.5f)
                {
                    arrived = true;
                    Debug.Log($"[ReturnToAnchor] Force arrived (Stuck/Stopped). Dist:{dist}");
                }
            }

            if (arrived)
            {
                active = false;
            }

            goal = MoveGoal.FromPosition(_anchorWorld);
            return true;
        }

        private Vector3 FindNearestKnotWorld()
        {
            var sp = splineContainer?.Spline;
            if (sp == null || sp.Count == 0) return transform.position;

            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < sp.Count; i++)
            {
                Vector3 world = splineContainer.transform.TransformPoint((Vector3)sp[i].Position);
                float d = Vector3.SqrMagnitude(new Vector3(world.x - transform.position.x, 0f, world.z - transform.position.z));
                if (d < bestD) { bestD = d; best = i; }
            }
            return splineContainer.transform.TransformPoint((Vector3)sp[best].Position);
        }
    }
}
