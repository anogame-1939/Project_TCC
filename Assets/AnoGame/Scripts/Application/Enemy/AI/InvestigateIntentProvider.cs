using UnityEngine;
using UnityEngine.AI;

namespace AnoGame.Application.Enemy.AI
{
    /// <summary>
    /// 「最後に目撃した位置」周辺を一定時間（TTL）だけ捜索する Intent Provider。
    /// - Activate(lastSeen, ttlSec) で開始
    /// - ランダムサンプリングで NavMesh 上の点へ順次移動（到達後は短い滞留→次点）
    /// - TTL 経過で自動的に ReturnToAnchor を起動して自分は停止
    /// - 再発見時などは NotifyFound() で即停止
    /// </summary>
    public sealed class InvestigateIntentProvider : MonoBehaviour, IIntentProvider
    {
        [Header("Intent Priority")]
        [SerializeField] private int priority = 60;
        public int Priority => priority;

        [Header("Activation State (runtime)")]
        [SerializeField] private bool active = false;

        [Header("Search Center (runtime)")]
        [SerializeField] private Vector3 center;   // last seen
        [SerializeField] private bool hasCenter = false;

        [Header("Behavior")]
        [Tooltip("捜索半径（lastSeen 周辺の最大距離）")]
        [SerializeField, Min(0.1f)] private float searchRadius = 6.0f;

        [Tooltip("中心の直近を避けたい場合の内側除外半径（0で無効）")]
        [SerializeField, Min(0f)] private float innerClearRadius = 0.5f;

        [Tooltip("各ポイントに到達してから留まる秒数")]
        [SerializeField, Min(0f)] private float dwellSecondsAtPoint = 0.8f;

        [Tooltip("ポイント到達判定距離（平面距離）")]
        [SerializeField, Min(0.05f)] private float arriveDistance = 0.6f;

        [Tooltip("新しいランダム目標を選ぶ最短インターバル（連続変更の暴れを抑制）")]
        [SerializeField, Min(0f)] private float minPickInterval = 0.25f;

        [Header("NavMesh Sampling")]
        [Tooltip("NavMesh.SamplePosition の探索半径")]
        [SerializeField, Min(0.01f)] private float navmeshSnapRadius = 1.0f;

        [Tooltip("1回の目標選定で試行するサンプル回数")]
        [SerializeField, Min(1)] private int sampleAttempts = 10;

        [Tooltip("サンプリングに失敗し続けた場合に半径へ掛ける縮小係数（例: 0.6）")]
        [SerializeField, Range(0.2f, 1.0f)] private float fallbackShrinkFactor = 0.6f;

        [Tooltip("使用する NavMesh エリアマスク（-1 = AllAreas）")]
        [SerializeField] private int areaMask = NavMesh.AllAreas;

        [Header("TTL / Escalation")]
        [Tooltip("Activate で ttlSec を 0 以下指定時に使うデフォルト TTL")]
        [SerializeField, Min(0f)] private float defaultTTL = 3.0f;

        [Tooltip("TTL 経過時に帰投を指示する先（任意）")]
        [SerializeField] private ReturnToAnchorIntentProvider returnToAnchor;

        // --- 内部状態 ---
        private Vector3 _currentTarget;
        private bool _hasTarget;
        private float _dwellTimer;
        private float _expireAt;
        private float _nextPickAllowedTime;

        // ========= Public API =========

        /// <summary>中心（lastSeen）と調査時間を与えて開始</summary>
        public void Activate(Vector3 lastSeenWorld, float ttlSeconds)
        {
            center = lastSeenWorld;
            hasCenter = true;
            active = true;

            float ttl = (ttlSeconds > 0f) ? ttlSeconds : defaultTTL;
            _expireAt = (ttl > 0f) ? Time.time + ttl : float.PositiveInfinity;

            _hasTarget = false;
            _dwellTimer = 0f;
            _nextPickAllowedTime = 0f;
        }

        /// <summary>外部から停止（再発見時など）</summary>
        public void Deactivate()
        {
            active = false;
            hasCenter = false;
            _hasTarget = false;
        }

        /// <summary>再発見通知：即停止して下位（Chase など）に明け渡す想定</summary>
        public void NotifyFound() => Deactivate();

        // ========= IIntentProvider =========

        public bool IsActive()
        {
            if (!active || !hasCenter) return false;

            // TTL経過で ReturnToAnchor へエスカレーション
            if (Time.time >= _expireAt)
            {
                active = false;
                if (returnToAnchor != null)
                    returnToAnchor.ActivateToNearest();
                return false;
            }
            return true;
        }

        public bool TryGetGoal(out MoveGoal goal)
        {
            goal = default;

            if (!IsActive())
                return false;

            // 到達 → 滞留 → 次の目標
            if (_hasTarget)
            {
                if (IsArrived(transform.position, _currentTarget, arriveDistance))
                {
                    if (_dwellTimer <= 0f)
                    {
                        _dwellTimer = dwellSecondsAtPoint;
                    }
                }

                if (_dwellTimer > 0f)
                {
                    _dwellTimer -= Time.deltaTime; // Router は FixedUpdate だが許容
                    // 滞留中はその場（＝現目標）を維持
                    goal = MoveGoal.FromPosition(_currentTarget);
                    return true;
                }
            }

            // 目標未設定 or 目標更新タイミング → 新規にランダムポイントを選ぶ
            if (!_hasTarget || Time.time >= _nextPickAllowedTime)
            {
                if (TryPickRandomInvestigatePoint(center, searchRadius, out _currentTarget))
                {
                    _hasTarget = true;
                    _nextPickAllowedTime = Time.time + minPickInterval;
                }
                else
                {
                    // サンプリングに失敗したら中心へ向かう（最小限のフォールバック）
                    _currentTarget = center;
                    _hasTarget = true;
                    _nextPickAllowedTime = Time.time + minPickInterval;
                }
            }

            goal = MoveGoal.FromPosition(_currentTarget);
            return true;
        }

        // ========= Helpers =========

        private bool IsArrived(Vector3 a, Vector3 b, float thresh)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b) <= Mathf.Max(0.01f, thresh);
        }

        /// <summary>
        /// 中心と半径を基準に、NavMesh 上の有効なポイントをランダム選定。
        /// 失敗が続くと半径を縮めて再試行。
        /// </summary>
        private bool TryPickRandomInvestigatePoint(Vector3 c, float radius, out Vector3 result)
        {
            result = c;

            float r = Mathf.Max(0.1f, radius);
            for (int ring = 0; ring < 3; ring++) // 半径を最大3段階縮めて試す
            {
                for (int i = 0; i < sampleAttempts; i++)
                {
                    // XY: (-1..1) の円、Yは0で XZ平面へ
                    Vector2 rand = Random.insideUnitCircle * r;

                    // 内側除外半径を適用（近すぎる点を避ける）
                    if (innerClearRadius > 0f && rand.sqrMagnitude < innerClearRadius * innerClearRadius)
                    {
                        // 内円に入ったら外周へ押し出す
                        rand = rand.normalized * Mathf.Max(innerClearRadius, 0.01f);
                    }

                    Vector3 candidate = new Vector3(c.x + rand.x, c.y, c.z + rand.y);

                    if (NavMesh.SamplePosition(candidate, out var hit, navmeshSnapRadius, areaMask))
                    {
                        result = hit.position;
                        return true;
                    }
                }
                r *= fallbackShrinkFactor; // 縮めて再試行
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!active && !hasCenter) return;

            // 中心＆半径
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            DrawDisc(center, searchRadius);
            if (innerClearRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.2f, 0f, 0.25f);
                DrawDisc(center, innerClearRadius);
            }

            // 現在の目標
            if (_hasTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentTarget, arriveDistance);
                Gizmos.DrawLine(transform.position, _currentTarget);
            }
        }

        private void DrawDisc(Vector3 c, float r)
        {
            const int k = 48;
            Vector3 prev = c + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= k; i++)
            {
                float ang = (i / (float)k) * Mathf.PI * 2f;
                Vector3 p = c + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
#endif
    }
}
