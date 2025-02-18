using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// Rigidbodyを使わず、コルーチンで「木が倒れる動き」を再現する
    /// </summary>
    public class CustomTreeRigidbody : MonoBehaviour
    {
        [Header("倒れるのにかかる時間 (秒)")]
        public float fallDuration = 2f;
        [Header("倒す角度 (度)")]
        public float fallAngle = 90f;

        // 木が倒れる軸をどこにするか(ローカルオフセット)
        // 例：根元をPivotにしたいならYを少しマイナスにする、など
        [Header("倒れる軸のPivotオフセット")]
        public Vector3 pivotOffset = Vector3.zero;

        private bool _isFalling = false;

        /// <summary>
        /// 外部から呼び出される「倒す」処理
        /// </summary>
        /// <param name="fellerPosition">倒す実行者の位置</param>
        public void Fall(Vector3 fellerPosition)
        {
            // すでに倒れている最中なら二重に開始しない
            if (_isFalling) { return; }

            _isFalling = true;
            StartCoroutine(FallCoroutine(fellerPosition));
        }

        /// <summary>
        /// 実際にコルーチンで回転させる
        /// </summary>
        private IEnumerator FallCoroutine(Vector3 fellerPosition)
        {
            // 1) 倒す軸を決める
            // 実行者(=feller)の位置から見て「反対側」に倒す場合
            //   direction = (木の位置 - fellerの位置)
            //   例：Vector3.up と direction の外積を使って回転軸を計算
            Vector3 direction = (transform.position - fellerPosition).normalized;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, direction).normalized;

            // 2) pivotのワールド座標を求める (transform.position + オフセット)
            //    ここが“根元”になるように調整
            Vector3 pivot = transform.position + pivotOffset;

            // 3) 実際にfallDuration秒かけて、fallAngle度 回転させる
            float elapsed = 0f;
            float currentAngle = 0f;

            while (elapsed < fallDuration)
            {
                // このフレームで回転させる角度 (度)
                float deltaAngle = (fallAngle / fallDuration) * Time.deltaTime;

                // RotateAround( pivot, 軸, 角度 ) で回転
                transform.RotateAround(pivot, rotationAxis, deltaAngle);

                // 進捗を更新
                currentAngle += deltaAngle;
                elapsed += Time.deltaTime;

                yield return null; // 次のフレームまで待つ
            }

            // 計算上、最後に微妙に角度が足りないことがあるので補正
            float remaining = fallAngle - currentAngle;
            if (remaining > 0f)
            {
                transform.RotateAround(pivot, rotationAxis, remaining);
            }

            // コルーチン終了
        }
    }
}
