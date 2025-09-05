// ===================================
// Provider: EventLock（演出など一時上書き）
//  - 中心に向かわせる最小実装（“その場うろうろ”は TODO）
//  - TTL 経過で自動解除
// ===================================
using UnityEngine;

namespace AnoGame.Application.Enemy.AI
{
    public class EventLockIntentProvider : MonoBehaviour, IIntentProvider
    {
        [SerializeField] private int priority = 100;

        [Header("状態")]
        [SerializeField] private bool active = false;
        [SerializeField] private Transform center;
        [SerializeField] private float ttlSeconds = 0f;
        private float _expireAt = 0f;

        public int Priority => priority;

        public bool IsActive()
        {
            if (!active) return false;
            if (ttlSeconds > 0f && Time.time >= _expireAt) { active = false; return false; }
            return center != null;
        }

        public bool TryGetGoal(out MoveGoal goal)
        {
            if (!IsActive()) { goal = default; return false; }
            // TODO: “その場でうろうろ”にするなら center 近傍のランダム NavMesh 点を一定間隔で更新
            goal = MoveGoal.FromPosition(center.position);
            return true;
        }

        // 外部から演出開始
        public void Activate(Transform centerTransform, float durationSeconds = 0f)
        {
            center = centerTransform;
            ttlSeconds = Mathf.Max(0f, durationSeconds);
            _expireAt = ttlSeconds > 0f ? Time.time + ttlSeconds : 0f;
            active = true;
        }

        public void Deactivate() => active = false;
    }
}
