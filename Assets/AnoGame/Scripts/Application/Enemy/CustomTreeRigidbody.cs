using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// Rigidbodyを使わず、コルーチンのみで木が倒れる動きを再現する
    /// </summary>
    public class CustomTreeRigidbody : MonoBehaviour
    {
        private bool _isFalling = false;

        /// <summary>
        /// 外部から呼び出される「木を倒す」メソッド
        /// </summary>
        /// <param name="fellerPosition">倒す実行者のワールド座標</param>
        /// <param name="fallDuration">倒れるのにかける時間(秒)</param>
        /// <param name="fallAngle">倒す角度(度)</param>
        /// <param name="pivotOffset">回転軸のオフセット(根元を軸にしたい場合など)</param>
        /// <param name="fallCurve">倒れる動き(0→1でどのくらい倒すか)を制御するアニメーションカーブ</param>
        public void Fall(Vector3 fellerPosition, float fallDuration, float fallAngle, Vector3 pivotOffset, AnimationCurve fallCurve)
        {
            // すでに倒れている最中なら二重に開始しない
            if (_isFalling) { return; }

            _isFalling = true;
            StartCoroutine(FallCoroutine(fellerPosition, fallDuration, fallAngle, pivotOffset, fallCurve));
        }

        /// <summary>
        /// コルーチンで段階的に木を回転させ、最終的に倒す
        /// </summary>
        private IEnumerator FallCoroutine(Vector3 fellerPosition, float fallDuration, float fallAngle, Vector3 pivotOffset, AnimationCurve fallCurve)
        {
            // 木から見た実行者との相対方向を求める
            Vector3 direction = (transform.position - fellerPosition).normalized;
            // 回転軸を計算 (上方向×direction)
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, direction).normalized;
            // 回転の中心(pivot)を、木の位置 + オフセット で算出
            Vector3 pivot = transform.position + pivotOffset;

            float elapsed = 0f;
            float currentAngle = 0f;

            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                // 0→1に正規化
                float t = Mathf.Clamp01(elapsed / fallDuration);

                // カーブから今の時点の「倒れ度合い」(0=未倒、1=全倒)を取得
                float curveValue = fallCurve.Evaluate(t);
                // 現在何度倒すかをLerpで算出
                float targetAngle = Mathf.Lerp(0f, fallAngle, curveValue);

                // 今フレームで回転させる差分角度
                float deltaAngle = targetAngle - currentAngle;
                transform.RotateAround(pivot, rotationAxis, deltaAngle);

                currentAngle = targetAngle;
                yield return null;
            }

            // 最終的に fallAngle 度まで倒れきった状態
        }
    }
}
