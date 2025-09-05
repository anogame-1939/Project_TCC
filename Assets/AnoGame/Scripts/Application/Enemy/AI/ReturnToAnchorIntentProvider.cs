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
        [SerializeField, Min(0.05f)] private float arriveDistance = 0.5f;

        private Vector3 _anchorWorld;

        public int Priority => priority;

        public void ActivateToNearest()
        {
            active = true;
            _anchorWorld = FindNearestKnotWorld();
        }

        public void Deactivate() => active = false;

        public bool IsActive() => active && splineContainer != null && splineContainer.Spline != null;

        public bool TryGetGoal(out MoveGoal goal)
        {
            if (!active) { goal = default; return false; }

            // 到達したら自動で解除（巡回など下位に明け渡す）
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = _anchorWorld;       b.y = 0f;
            if (Vector3.Distance(a, b) <= arriveDistance)
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
