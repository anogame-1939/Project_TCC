using UnityEngine;

namespace AnoGame.Application.Direction
{
    [RequireComponent(typeof(Rigidbody))]
    public class CanRoller : MonoBehaviour
    {
        [Header("Roll Settings")]
        [Tooltip("転がす力（小さめでOK）")]
        public float rollForce = 2f;

        [Tooltip("転がす方向（デフォルトは前方向）")]
        public Vector3 rollDirection = Vector3.forward;

        [Tooltip("転がした後に少しだけ減速させる")]
        public float drag = 0.5f;

        private Rigidbody _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// 外部からちょっと転がす用のメソッド
        /// </summary>
        [Button]
        public void RollOnce()
        {
            // 転がす方向を正規化
            Vector3 dir = rollDirection.normalized;

            // 少しだけ力を加える
            _rb.AddForce(dir * rollForce, ForceMode.Impulse);

            // ゆるやかに止まるようにドラッグを増やす
            _rb.drag = drag;
            _rb.angularDrag = drag * 0.5f;
        }
    }
}