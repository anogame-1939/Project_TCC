using UnityEngine;
using Unity.TinyCharacterController.Interfaces.Core;
using Unity.TinyCharacterController.Interfaces.Components;

namespace AnoGame.Application.Player.Control
{
    /// <summary>
    /// ゲームイベント中などに Move/Turn の主導権を奪うためのコンポーネント。
    /// IMove/ITurn を高優先度で出力し、プレイヤー入力よりも先に適用される。
    /// テストしやすいように移動/注視の挙動を切替可能。
    /// </summary>
    [AddComponentMenu("AnoGame/Control/" + nameof(EventLockControl))]
    public sealed class EventLockControl : MonoBehaviour,
        IMove, ITurn, IPriorityLifecycle<IMove>, IPriorityLifecycle<ITurn>
    {
        private IWarp _warp;
        
        [Header("Priority (他のMove/Turnより高く)")]
        [SerializeField] int movePriority = 1000;
        [SerializeField] int turnPriority = 1000;

        [Header("Activation")]
        [SerializeField] bool _isActive;
        public bool IsActive => _isActive;

        // ===== Move 設定 =====
        public enum MoveBehavior { Freeze, ConstantVelocity, ToPoint, FollowTransform }
        [Header("Move")]
        [SerializeField] MoveBehavior moveBehavior = MoveBehavior.Freeze;
        [SerializeField] float moveSpeed = 2.0f;
        [SerializeField] Vector3 constantDirection = Vector3.forward;
        [SerializeField] Transform followTarget;
        [SerializeField] Vector3 targetPoint;
        [SerializeField, Min(0f)] float stopDistance = 0.05f;

        // ===== Turn 設定 =====
        public enum TurnBehavior { Keep, FaceMoveDirection, FaceTarget }
        [Header("Turn")]
        [SerializeField] TurnBehavior turnBehavior = TurnBehavior.FaceMoveDirection;
        [SerializeField] Transform lookAtTarget;

        // ---- IMove / ITurn 優先度 ----
        int IPriority<IMove>.Priority => _isActive ? movePriority : 0;
        int IPriority<ITurn>.Priority => _isActive ? turnPriority : 0;

        // ---- IMove: 速度出力 ----
        public Vector3 MoveVelocity
        {
            get
            {
                if (!_isActive) return Vector3.zero;

                switch (moveBehavior)
                {
                    case MoveBehavior.Freeze:
                        _lastVelocity = Vector3.zero;
                        return Vector3.zero;

                    case MoveBehavior.ConstantVelocity:
                        {
                            var v = constantDirection.sqrMagnitude > 0f
                                ? constantDirection.normalized * moveSpeed
                                : Vector3.zero;
                            _lastVelocity = v;
                            return v;
                        }

                    case MoveBehavior.ToPoint:
                        {
                            var to = targetPoint - transform.position;
                            to.y = 0f;
                            var dist = to.magnitude;
                            if (dist <= stopDistance)
                            {
                                _lastVelocity = Vector3.zero;
                                return Vector3.zero;
                            }
                            var v = to / Mathf.Max(dist, 0.0001f) * moveSpeed;
                            _lastVelocity = v;
                            return v;
                        }

                    case MoveBehavior.FollowTransform:
                        {
                            if (followTarget == null)
                            {
                                _lastVelocity = Vector3.zero;
                                return Vector3.zero;
                            }
                            var to = followTarget.position - transform.position;
                            to.y = 0f;
                            var dist = to.magnitude;
                            if (dist <= stopDistance)
                            {
                                _lastVelocity = Vector3.zero;
                                return Vector3.zero;
                            }
                            var v = to / Mathf.Max(dist, 0.0001f) * moveSpeed;
                            _lastVelocity = v;
                            return v;
                        }
                }
                _lastVelocity = Vector3.zero;
                return Vector3.zero;
            }
        }

        // ---- ITurn: 目標Yaw角度（度） ----
        float ITurn.YawAngle
        {
            get
            {
                if (!_isActive) return transform.eulerAngles.y;

                switch (turnBehavior)
                {
                    case TurnBehavior.Keep:
                        return transform.eulerAngles.y;

                    case TurnBehavior.FaceMoveDirection:
                        {
                            var v = _lastVelocity;
                            v.y = 0f;
                            if (v.sqrMagnitude < 0.0001f) return transform.eulerAngles.y;
                            var angle = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                            return angle; // Z前方前提：Yaw=atan2(x,z)
                        }

                    case TurnBehavior.FaceTarget:
                        {
                            if (lookAtTarget == null) return transform.eulerAngles.y;
                            var to = lookAtTarget.position - transform.position;
                            to.y = 0f;
                            if (to.sqrMagnitude < 0.0001f) return transform.eulerAngles.y;
                            var angle = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                            return angle;
                        }
                }
                return transform.eulerAngles.y;
            }
        }

        public int TurnSpeed => 0;

        void Awake()
        {
            TryGetComponent(out _warp);
        }

        public bool TryWarp(Vector3 position) {
            if (_warp == null) return false;
            _warp.Warp(position);
            return true;
        }

        public bool TryWarp(Vector3 position, Vector3 faceDir) {
            if (_warp == null) return false;
            _warp.Warp(position, faceDir);   // faceDir==Vector3.zero なら現向き維持（BrainBase仕様）
            return true;
        }

        public bool TryWarp(Vector3 position, Quaternion rot)
        {
            if (_warp == null) return false;

            // 位置を更新
            _warp.Warp(position);

            // 向きは Quaternion で更新
            _warp.Warp(rot);

            return true;
        }

        // ---- ライフサイクル（必要ならここでアニメ切替など） ----
        public void OnAcquireHighestPriority() { /* 例: Animator.SetBool("IsMove", false); */ }
        public void OnLoseHighestPriority() { /* 復帰処理 */ }
        public void OnUpdateWithHighestPriority(float dt) { /* 毎フレ処理があれば */ }

        // ---- テスト用API（外部から操作しやすいユーティリティ） ----
        public void BeginLock() => _isActive = true;
        public void EndLock(bool resetState = true)
        {
            _isActive = false;

            // ★ デフォルトで状態クリアしておく（勝手移動の再発防止）
            if (resetState)
                ResetMoveTurnState(snapTargetToCurrentPosition: true);
        }

        private void ResetMoveTurnState(bool snapTargetToCurrentPosition)
        {
            // Move 側の初期化（安全なデフォルト）
            moveBehavior = MoveBehavior.Freeze;
            moveSpeed = 2.0f;                  // 既定を維持するならそのまま
            constantDirection = Vector3.zero;  // 誤発進防止
            followTarget = null;
            stopDistance = Mathf.Max(0f, stopDistance); // そのまま維持 or 既定に戻す

            // 「0 に戻す」か「現在位置に寄せる」かを選択
            targetPoint = snapTargetToCurrentPosition ? transform.position : Vector3.zero;

            _lastVelocity = Vector3.zero;

            // Turn 側の初期化（必要なら）
            lookAtTarget = null;
            // デフォルトの向きモードに戻す（Keepが自然ならそちらでもOK）
            turnBehavior = TurnBehavior.FaceMoveDirection;
        }

        public void Freeze() { moveBehavior = MoveBehavior.Freeze; }
        public void MoveConstant(Vector3 worldDir, float speed)
        {
            moveBehavior = MoveBehavior.ConstantVelocity;
            constantDirection = worldDir;
            moveSpeed = speed;
        }
        public void MoveToPoint(Vector3 worldPoint, float speed, float stopDist = 0.05f)
        {
            moveBehavior = MoveBehavior.ToPoint;
            targetPoint = worldPoint;
            moveSpeed = speed;
            stopDistance = Mathf.Max(0f, stopDist);

            Debug.Log($"[EventLock] MoveToPoint set: {targetPoint}", this);
        }
        public void Follow(Transform t, float speed, float stopDist = 0.05f)
        {
            moveBehavior = MoveBehavior.FollowTransform;
            followTarget = t;
            moveSpeed = speed;
            stopDistance = Mathf.Max(0f, stopDist);

            Debug.Log($"[EventLock] Gizmo targetPoint: {targetPoint}", this);
        }

        public void LookKeep() => turnBehavior = TurnBehavior.Keep;
        public void LookFaceMove() => turnBehavior = TurnBehavior.FaceMoveDirection;
        public void LookAt(Transform t) { lookAtTarget = t; turnBehavior = TurnBehavior.FaceTarget; }

        // ---- Context Menu（右クリック/︙メニューから即操作） ----
        [ContextMenu("EventLock/Begin Lock")] void Ctx_Begin() => BeginLock();
        [ContextMenu("EventLock/End Lock")] void Ctx_End() => EndLock();
        [ContextMenu("EventLock/Freeze")] void Ctx_Freeze() => Freeze();
        [ContextMenu("EventLock/MoveConstant (Forward x 2 m/s)")]
        void Ctx_MoveConst() => MoveConstant(transform.forward, 2f);
        [ContextMenu("EventLock/MoveToPoint (+3m forward)")]
        void Ctx_MoveToPt() => MoveToPoint(transform.position + transform.forward * 3f, 2f);
        [ContextMenu("EventLock/Look: Keep")] void Ctx_LookKeep() => LookKeep();
        [ContextMenu("EventLock/Look: FaceMove")] void Ctx_LookMove() => LookFaceMove();

        // ---- 内部保持 ----
        private Vector3 _lastVelocity = Vector3.zero;
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPoint, 0.1f);
            Debug.Log($"[EventLock] Gizmo targetPoint: {targetPoint}", this);
            Gizmos.DrawLine(transform.position, new Vector3(targetPoint.x, transform.position.y, targetPoint.z));
        }
    }
}
