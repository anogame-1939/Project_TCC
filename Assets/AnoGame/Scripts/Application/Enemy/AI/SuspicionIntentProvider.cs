using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnoGame.Application.Enemy.AI
{
    /// <summary>
    /// 疑惑（Suspicion）ステート用プロバイダ。
    /// 指定位置（LastSeen）を一定時間（TTL）注視し続ける。
    /// 移動はせず、その場で回転して対象を見る（あるいは単にWait）。
    /// </summary>
    public sealed class SuspicionIntentProvider : MonoBehaviour, IIntentProvider
    {
        [Header("Intent Priority")]
        [SerializeField] private int priority = 65; // Investigate(60) < Suspicion(65) < Chase(70)
        public int Priority => priority;

        [Header("Runtime State")]
        [SerializeField] private bool active = false;
        [SerializeField] private Vector3 lookTarget;

        [Header("Settings")]
        [SerializeField] private float defaultTTL = 2.0f;
        [Tooltip("注視時の回転速度")]
        [SerializeField] private float rotateSpeed = 5.0f;

        public event Action OnExpired;
        public event Action OnDeactivated;

        private float _expireAt;

        // IIntentProvider
        public bool IsActive()
        {
            if (!active) return false;

            // TTLチェック
            if (Time.time >= _expireAt)
            {
                Debug.Log($"[Suspicion] Expired! Time: {Time.time}, ExpireAt: {_expireAt}");
                Deactivate(expired: true);
                return false;
            }
            return true;
        }

        public bool TryGetGoal(out MoveGoal goal)
        {
            goal = default;
            if (!IsActive()) return false;

            // 移動はしない（現在位置）
            goal = MoveGoal.FromPosition(transform.position);
            // goal.Facing = lookTarget; // Router側が未対応なら一旦コメントアウト、あるいは transform.LookAt をここで呼ぶ（非推奨だが）

            // 簡易実装: Routerが更新されるまでの間、自分で向く
            if (lookTarget != Vector3.zero)
            {
                var dir = lookTarget - transform.position;
                if (dir.sqrMagnitude > 0.1f)
                {
                    dir.y = 0;
                    var rot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotateSpeed);
                }
            }

            return true;
        }

        // API
        public void Activate(Vector3 targetPos, float ttl = -1f)
        {
            lookTarget = targetPos;
            active = true;

            float t = (ttl > 0f) ? ttl : defaultTTL;
            _expireAt = Time.time + t;
        }

        public void Deactivate(bool expired = false)
        {
            if (!active) return;
            active = false;

            if (expired)
            {
                OnExpired?.Invoke();
            }
            else
            {
                OnDeactivated?.Invoke();
            }
        }
    }
}
