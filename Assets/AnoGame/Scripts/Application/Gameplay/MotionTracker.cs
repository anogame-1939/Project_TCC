using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Gameplay
{
    // プレイヤーに付与
    public class MotionTracker : MonoBehaviour
    {
        [field: SerializeField, Min(0.1f)] public float Smooth = 10f; // 大きいほど早く追従
        public Vector3 SmoothedVelocity { get; private set; }
        public Vector3 SmoothedForward { get; private set; }

        Vector3 _lastPos;

        void Awake() => _lastPos = transform.position;

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 v = (transform.position - _lastPos) / dt; v.y = 0f;

            // EMA: 1 - e^{-k dt}
            float a = 1f - Mathf.Exp(-Smooth * dt);
            SmoothedVelocity = Vector3.Lerp(SmoothedVelocity, v, a);

            Vector3 f = SmoothedVelocity; f.y = 0f;
            if (f.sqrMagnitude > 0.01f) SmoothedForward = f.normalized;

            _lastPos = transform.position;
        }
    }
}