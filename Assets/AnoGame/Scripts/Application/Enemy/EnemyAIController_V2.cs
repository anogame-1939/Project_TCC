using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Enmemy.Control // ← 既存に合わせて綴りそのまま
{
    /// <summary>
    /// MoveNavmeshControl 経由で移動する最小AI。
    /// 追跡 or Spline巡回 をインスペクターで切替可能。
    /// </summary>
    public class EnemyAIController_V2 : MonoBehaviour
    {
        public enum BehaviorMode { ChasePlayer, PatrolSpline }

        [Header("共通")]
        [SerializeField] private BehaviorMode behavior = BehaviorMode.ChasePlayer;
        [SerializeField] private MoveNavmeshControl moveNavmeshControl;

        [Header("プレイヤー追跡")]
        [SerializeField] private string playerTag = "Player";

        [Header("Spline パトロール")]
        [SerializeField] private SplineContainer splineContainer;
        [Tooltip("この距離以下で次のウェイポイントへ進む")]
        [SerializeField, Min(0.05f)] private float arriveDistance = 0.5f;
        [Tooltip("各Knotで停止する秒数（0で即時遷移）")]
        [SerializeField, Min(0f)] private float waitAtKnotSeconds = 0f;
        [Tooltip("最後のKnotの次は先頭に戻る（true）/ その場で停止（false）")]
        [SerializeField] private bool loopPatrol = true;

        // 内部
        private GameObject player;
        private NavMeshAgent agent;           // 到達判定のために読むだけ（移動は moveNavmeshControl 経由）
        private int currentKnotIndex = 0;
        private Vector3 currentTargetWorld;
        private float waitTimer = 0f;

        private void Awake()
        {
            if (moveNavmeshControl == null)
                moveNavmeshControl = GetComponent<MoveNavmeshControl>();

            agent = GetComponentInChildren<NavMeshAgent>(); // いるなら到達判定に利用
        }

        private void Start()
        {
            if (behavior == BehaviorMode.PatrolSpline)
                ResetPatrolAndSetFirstTarget();
        }

        private void Update()
        {
            // ここでは何もしない（将来の拡張用）
        }

        private void FixedUpdate()
        {
            if (moveNavmeshControl == null) return;

            if (behavior == BehaviorMode.ChasePlayer)
            {
                if (player == null)
                    player = GameObject.FindWithTag(playerTag);

                if (player != null)
                    moveNavmeshControl.SetTargetPosition(player.transform.position);
                return;
            }

            // --- Spline パトロール ---
            if (splineContainer == null || splineContainer.Spline == null || splineContainer.Spline.Count == 0)
                return;

            // Knot到達待機中
            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                // 待機中も向かせたい/微調整したければここでやる
                return;
            }

            // 目的地を維持（毎FixedUpdateで送り続ける）
            moveNavmeshControl.SetTargetPosition(currentTargetWorld);

            // 到達判定
            bool reached = false;

            if (agent != null && !agent.pathPending)
            {
                // NavMeshAgent がある場合はパス上の残距離で判定
                if (agent.remainingDistance <= arriveDistance)
                    reached = true;
            }
            else
            {
                // フォールバック：直線距離
                float planar = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                                new Vector3(currentTargetWorld.x, 0f, currentTargetWorld.z));
                if (planar <= arriveDistance)
                    reached = true;
            }

            if (reached)
                AdvanceToNextKnot();
        }

        // =============== パトロール補助 ===============

        private void ResetPatrolAndSetFirstTarget()
        {
            currentKnotIndex = 0;
            if (TryGetKnotWorldPosition(currentKnotIndex, out var wp))
            {
                currentTargetWorld = wp;
                moveNavmeshControl.SetTargetPosition(currentTargetWorld);
                waitTimer = waitAtKnotSeconds;
            }
        }

        private void AdvanceToNextKnot()
        {
            int count = splineContainer.Spline.Count;
            if (count <= 0) return;

            int next = currentKnotIndex + 1;
            if (next >= count)
            {
                if (!loopPatrol) return; // ループしないなら最後で停止
                next = 0;
            }

            currentKnotIndex = next;

            if (TryGetKnotWorldPosition(currentKnotIndex, out var wp))
            {
                currentTargetWorld = wp;
                moveNavmeshControl.SetTargetPosition(currentTargetWorld);
                waitTimer = waitAtKnotSeconds;
            }
        }

        private bool TryGetKnotWorldPosition(int index, out Vector3 worldPos)
        {
            worldPos = default;
            var spline = splineContainer?.Spline;
            if (spline == null || index < 0 || index >= spline.Count) return false;

            // Knot.Position はローカル座標なのでワールドへ変換
            Vector3 local = (Vector3)spline[index].Position;
            worldPos = splineContainer.transform.TransformPoint(local);
            return true;
        }

        // =============== ランタイム切替API（任意） ===============

        /// <summary>挙動をランタイムで切り替える（外部から呼べます）</summary>
        public void SetBehavior(BehaviorMode newMode)
        {
            if (behavior == newMode) return;
            behavior = newMode;

            if (behavior == BehaviorMode.PatrolSpline)
                ResetPatrolAndSetFirstTarget();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (splineContainer == null || splineContainer.Spline == null) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < splineContainer.Spline.Count; i++)
            {
                Vector3 local = (Vector3)splineContainer.Spline[i].Position;
                Vector3 world = splineContainer.transform.TransformPoint(local);
                Gizmos.DrawSphere(world, 0.15f);
            }

            // 現在のターゲット
            if (behavior == BehaviorMode.PatrolSpline)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentTargetWorld, arriveDistance);
            }
        }
#endif
    }
}
