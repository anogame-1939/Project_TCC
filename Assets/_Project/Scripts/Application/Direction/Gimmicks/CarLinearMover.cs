using UnityEngine;

namespace AnoGame.Application.Direction.Gimmicks
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AnoGame/Direction/Car Linear Mover (Timeline Activate)")]
    public sealed class CarLinearMover : MonoBehaviour
    {
        [Header("参照 (どちらも必須)")]
        [Tooltip("開始位置（出現位置）")]
        public Transform startPoint;
        [Tooltip("移動先")]
        public Transform endPoint;

        [Header("移動設定")]
        [Tooltip("移動にかける時間（秒）")]
        [Min(0f)] public float duration = 2.0f;
        [Tooltip("タイムスケール非依存で進めるか（Timeline再生速度の影響を受けにくくしたい場合はON）")]
        public bool useUnscaledTime = false;

        // startPointを指定しない場合のために、初期配置を覚えておく
        Vector3 _fallbackStartPos;
        bool _hasAwake;

        void Awake()
        {
            _fallbackStartPos = transform.position;
            _hasAwake = true;
        }

        void OnEnable()
        {
            // 有効化されたら毎回、開始位置にワープ → 移動を開始
            if (startPoint != null)
                transform.position = startPoint.position;
            else if (_hasAwake)
                transform.position = _fallbackStartPos;

            if (isActiveAndEnabled && endPoint != null && duration > 0f)
                StartCoroutine(MoveRoutine());
        }

        void OnDisable()
        {
            // 無効化されたら移動を停止（次回OnEnableでやり直し）
            StopAllCoroutines();
        }

        System.Collections.IEnumerator MoveRoutine()
        {
            Vector3 from = transform.position;
            Vector3 to = endPoint.position;

            float t = 0f;
            while (t < 1f)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += (duration <= 0f) ? 1f : dt / duration; // 念のための保険
                transform.position = Vector3.LerpUnclamped(from, to, Mathf.Clamp01(t));
                yield return null;
            }
            transform.position = to; // 誤差吸収
        }
    }
}