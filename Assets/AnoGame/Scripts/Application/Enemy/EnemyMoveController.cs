using Unity.TinyCharacterController.Control;
using UnityEngine;

namespace AnoGame.Application.Enemy
{
    // NOTE:実質アニメーションコントローラー
    public class EnemyMoveController : MonoBehaviour
    {
        /// <summary>
        /// ・小数点第3位まで丸めた座標同士で比較する
        /// ・差分が threshold 未満なら 0 とみなす
        /// ・速度が threshold を超えたら移動中と判定
        /// ・移動先に向かせる（XZ平面を想定）
        /// </summary>
        [SerializeField] private float threshold = 0.1f;

        private Animator animator;
        private Vector3 previousPosition;

        private GameObject _targetObject;
        public void SetTargetObject(GameObject targetObject)
        {
            _targetObject = targetObject;
        }

        private void Awake()
        {
            animator = transform.GetChild(0).GetComponent<Animator>();
            previousPosition = transform.position;
        }

        private void Update()
        {
            if (_targetObject != null)
            {
                GetComponent<MoveNavmeshControl>().SetTargetPosition(_targetObject.transform.position);
            }

            // 1) 現在の座標を小数点第3位で丸める
            Vector3 currentPosition = RoundVector3(transform.position, 3);

            // 2) 前回の座標も小数点第3位で丸める
            Vector3 prevPosition = RoundVector3(previousPosition, 3);

            // 3) 差分を計算
            Vector3 displacement = currentPosition - prevPosition;
            float distance = displacement.magnitude;

            // 4) デッドゾーン適用: 差分が threshold 未満なら移動量を 0 とみなす
            if (distance < threshold)
            {
                displacement = Vector3.zero;
            }

            // 5) 速度を計算 (units/second)
            float speed = displacement.magnitude / Time.deltaTime;

            // 6) 速度が threshold を超えていれば移動中と判定
            bool isMoving = speed > 0;

            // --- 追加: 移動先にキャラクターを向かせる ---
            // displacement が 0 でなければ、その方向に回転させる
            if (displacement.sqrMagnitude > 0.000001f)
            {
                // XZ 平面のみで回転させる場合、y 成分を 0 に
                Vector3 direction = new Vector3(displacement.x, 0f, displacement.z);
                if (direction.sqrMagnitude > 0.000001f)
                {
                    // direction を正面として向く
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }

            // Debug.Log($"displacement: {displacement}, distance: {distance}, Speed: {speed}, IsMoving: {isMoving}");

            // 7) Animator のパラメータを更新
            animator.SetBool("IsMove", isMoving);

            // 8) 次回のために座標を保存
            previousPosition = currentPosition;
        }

        /// <summary>
        /// Vector3 を小数点第 decimals 位で四捨五入する
        /// </summary>
        private Vector3 RoundVector3(Vector3 source, int decimals)
        {
            return new Vector3(
                RoundFloat(source.x, decimals),
                RoundFloat(source.y, decimals),
                RoundFloat(source.z, decimals)
            );
        }

        /// <summary>
        /// float 値を小数点第 decimals 位で四捨五入する
        /// </summary>
        private float RoundFloat(float value, int decimals)
        {
            double scale = System.Math.Pow(10, decimals);
            return (float)(System.Math.Round(value * scale) / scale);
        }
    }
}
