#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace AnoGame
{
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    public class SplineColorGizmo : MonoBehaviour
    {
        [Header("表示切替")]
        public bool showGizmo = true; // ← InspectorからON/OFFできる

        [Header("描画設定")]
        public Color gizmoColor = Color.red;
        [Range(0.001f, 0.2f)] public float stepSize = 0.05f;

        void OnDrawGizmos()
        {
            if (!showGizmo) return; // ← OFFなら描かない

            var container = GetComponent<SplineContainer>();
            if (!container || container.Spline == null) return;

            var spline = container.Spline;
            Gizmos.color = gizmoColor;

            float3 offset = (float3)container.transform.position;

            Vector3 prev = (Vector3)(spline.EvaluatePosition(0f) + offset);

            for (float t = stepSize; t <= 1f; t += stepSize)
            {
                Vector3 curr = (Vector3)(spline.EvaluatePosition(t) + offset);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }
    }
}
#endif
