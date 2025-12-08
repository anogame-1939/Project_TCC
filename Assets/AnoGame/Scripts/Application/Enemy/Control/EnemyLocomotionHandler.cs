using UnityEngine;
using UnityEngine.AI;
using AnoGame.Application.Player.Control; // For EventLockControl

namespace AnoGame.Application.Enemy.Control
{
    /// <summary>
    /// Enemy用の移動制御ハンドラ。
    /// TCCのCharacterBrain/MoveNavmeshControlの代わりに使用する。
    /// NavMeshAgent (通常移動) と EventLockControl (イベント演出) の調停を行う。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("AnoGame/Enemy/Control/" + nameof(EnemyLocomotionHandler))]
    public class EnemyLocomotionHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private EventLockControl eventLockControl;

        [Header("Settings")]
        [SerializeField] private bool syncPosition = true;
        [SerializeField] private bool syncRotation = true;

        /// <summary>
        /// 現在の速度ベクトル（アニメーション用）
        /// </summary>
        public Vector3 Velocity => _velocity;

        private Vector3 _velocity;
        private bool _wasEventActive;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (eventLockControl == null) eventLockControl = GetComponent<EventLockControl>();
        }

        private void Start()
        {
            // イベント制御の初期化
            eventLockControl.BeginLock();

            // 初期化時に強制的に設定を適用
            // （TCCのPrefab設定で agent.updatePosition が false になっている可能性が高いため）
            if (agent != null)
            {
                agent.updatePosition = syncPosition;
                agent.updateRotation = syncRotation;
            }
        }

        private void Update()
        {
            bool isEventActive = eventLockControl != null && eventLockControl.IsActive;

            // モード切替時の初期化処理
            if (isEventActive != _wasEventActive)
            {
                if (isEventActive)
                {
                    // イベント開始: NavMeshAgentの制御を停止
                    if (agent.isActiveAndEnabled)
                    {
                        agent.updatePosition = false;
                        agent.updateRotation = false;
                        agent.isStopped = true;
                    }
                }
                else
                {
                    // イベント終了: NavMeshAgentの制御を再開
                    if (agent.isActiveAndEnabled)
                    {
                        // NavMesh上の位置を現在位置に合わせる（ワープ回避）
                        agent.Warp(transform.position);
                        agent.updatePosition = syncPosition;
                        agent.updateRotation = syncRotation;
                        agent.isStopped = false;
                    }
                }
                _wasEventActive = isEventActive;
            }

            if (isEventActive)
            {
                // --- イベント制御モード ---
                // EventLockControl からの指示を適用
                // EventLockControl は IMove/ITurn として振る舞うが、ここでは直接値を取れるなら取る
                // 実装に合わせて MoveVelocity プロパティなどを参照

                Vector3 moveVel = eventLockControl.MoveVelocity;

                // 回転
                // EventLockControl自体がTransformを回しているなら不要だが、
                // TCCのMoveNavmeshControlはBrain経由で回していた。
                // EventLockControlの実装を見ると、IMove/ITurnを返すだけで自力でTransformを回していない設計に見える（Brain依存）。
                // よって、ここで適用する必要がある。

                // 移動適用
                if (Mathf.Abs(moveVel.sqrMagnitude) > 1e-6f)
                {
                    // CharacterControllerがないのでTransform移動
                    transform.position += moveVel * Time.deltaTime;
                    _velocity = moveVel;
                }
                else
                {
                    _velocity = Vector3.zero;
                }

                // 回転適用 (ITurn相当)
                // EventLockControl から公開された TargetYawAngle プロパティを使用（TCC依存回避）
                if (eventLockControl != null)
                {
                    float targetYaw = eventLockControl.TargetYawAngle;
                    // TCC Brainのような補間はせず、EventLockの値を正として適用
                    transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                }
            }
            else
            {
                // --- 通常（NavMesh）モード ---
                if (agent != null && agent.isActiveAndEnabled)
                {
                    // NavMeshAgentがTransformを動かしているはず
                    _velocity = agent.velocity;

                    // クリープ対策: 目的地についているならVelocityをゼロに見せる（アニメ用）
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        if (agent.velocity.sqrMagnitude < 0.1f)
                        {
                            _velocity = Vector3.zero;
                        }
                    }
                }
            }
        }

        public void SetDestination(Vector3 target)
        {
            if (eventLockControl != null && eventLockControl.IsActive)
            {
                // イベント中は目的地設定を無視（もしくは保留）
                return;
            }

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(target);
            }
        }

        public void Warp(Vector3 position, Vector3 direction)
        {
            if (agent != null)
            {
                agent.Warp(position);
                agent.transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                transform.position = position;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        public void Warp(Vector3 position)
        {
            if (agent != null)
            {
                agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }
    }
}
