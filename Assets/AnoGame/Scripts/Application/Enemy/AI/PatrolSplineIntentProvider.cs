// ===================================
// Provider: Spline 巡回（最小）
// ===================================
using UnityEngine;
using UnityEngine.Splines;

namespace AnoGame.Application.Enemy.AI
{
    public class PatrolSplineIntentProvider : MonoBehaviour, IIntentProvider
    {
        [SerializeField] private int priority = 40;
        [SerializeField] private bool active = true;

        [Header("Spline")]
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField, Min(0.05f)] private float arriveDistance = 0.5f;
        [SerializeField] private bool loop = true;

        private int _currentIndex = 0;
        private Vector3 _currentWorld;

        public int Priority => priority;

        private bool TryGetKnotWorld(int index, out Vector3 pos)
        {
            pos = default;
            var sp = splineContainer?.Spline;
            if (sp == null || sp.Count == 0 || index < 0 || index >= sp.Count) return false;
            Vector3 local = (Vector3)sp[index].Position;
            pos = splineContainer.transform.TransformPoint(local);
            return true;
        }

        public bool IsActive()
        {
            if (!active) return false;
            return (splineContainer != null && splineContainer.Spline != null && splineContainer.Spline.Count > 0);
        }

        public bool TryGetGoal(out MoveGoal goal)
        {
            // 初回セット
            if (_currentWorld == default || float.IsNaN(_currentWorld.x))
            {
                if (!TryGetKnotWorld(_currentIndex, out _currentWorld)) { goal = default; return false; }
            }

            // 到達チェック（平面距離の最小実装）
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = _currentWorld;     b.y = 0f;
            if (Vector3.Distance(a, b) <= arriveDistance)
            {
                int next = _currentIndex + 1;
                var count = splineContainer.Spline.Count;
                if (next >= count) next = loop ? 0 : _currentIndex;
                _currentIndex = next;
                TryGetKnotWorld(_currentIndex, out _currentWorld);
            }

            goal = MoveGoal.FromPosition(_currentWorld);
            return true;
        }

        public void SetActive(bool v) => active = v;
    }
}
