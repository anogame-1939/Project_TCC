using UnityEngine;

namespace AnoGame
{
    public class FlowLookAheadAnchor : MonoBehaviour
    {
        [Header("Follow")]
        public Transform followTarget;
        public Vector3 followOffset = Vector3.zero;

        [Header("Flow Settings")]
        public Vector3 flowDir = Vector3.down; // 花びらの平均流れ方向（重力方向）
        [Min(0f)] public float lookAheadTime = 0.8f; // 先読み秒数（0.6〜1.2で調整）
        public float maxLeadDistance = 20f;   // 前倒し距離の上限

        Vector3 _prevCamPos;
        bool _initialized;

        void Start()
        {
            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (followTarget == null) return;

            var camPos = followTarget.position;

            // カメラ速度（1フレーム目は0扱い）
            Vector3 vCam = Vector3.zero;
            if (_initialized)
            {
                float dt = Mathf.Max(Time.deltaTime, 1e-5f);
                vCam = (camPos - _prevCamPos) / dt;
            }
            _prevCamPos = camPos;
            _initialized = true;

            // カメラが「下流（-flowDir）」に進んでいる成分だけ見る
            Vector3 flowN = flowDir.sqrMagnitude > 0f ? flowDir.normalized : Vector3.down;
            float vDownstream = Vector3.Dot(vCam, -flowN); // 下流へ進む速度（m/s）
            float lead = Mathf.Clamp(vDownstream * lookAheadTime, 0f, maxLeadDistance);

            // 上流側へ前倒し配置
            Vector3 target = camPos + followOffset - flowN * lead;
            transform.position = target;
        }
    }
}
