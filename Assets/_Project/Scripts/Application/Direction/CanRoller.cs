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
        /// <summary>
        /// 指定したTransformから離れる方向に転がす
        /// </summary>
        /// <param name="from">転がす力の発生源（プレイヤーなど）</param>
        public void RollFrom(Transform from)
        {
            if (from == null) return;

            // 自身と対象の位置関係から方向を算出（Y軸は無視して水平方向のみ）
            Vector3 direction = transform.position - from.position;
            direction.y = 0f;

            // 重なっている場合などはfromの向いている方向にする
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = from.forward;
                direction.y = 0f;
            }

            rollDirection = direction.normalized;
            RollOnce();
        }
    }
}