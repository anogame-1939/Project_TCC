using UnityEngine;

namespace AnoGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Transform))]
    public class SmoothFollowAnchor : MonoBehaviour
    {
        [Header("Follow (カメラに追従)")]
        public Transform followTarget;                    // 通常はワールドを描画しているカメラ
        public Vector3 followOffset = Vector3.zero;       // カメラ基準のオフセット
        [Min(0f)] public float followSmoothTime = 0.25f;  // 追従の遅延（加速を粒子に乗せない）

        // SmoothDamp用の速度キャッシュ（オフセットではない）
        Vector3 _smoothVelocity;

        void Start()
        {
            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (followTarget == null) return;

            var target = followTarget.position + followOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position, target, ref _smoothVelocity, followSmoothTime);
        }
    }
}
