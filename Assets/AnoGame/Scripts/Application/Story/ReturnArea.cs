using UnityEngine;
using System.Collections;
using Unity.TinyCharacterController.Control; // ← MoveControl がある名前空間

namespace AnoGame.Application.Story
{
    [RequireComponent(typeof(Collider))]
    public class ReturnArea : MonoBehaviour
    {
        [SerializeField]
        private Transform returnPosition; // プレイヤーを戻す地点

        // トリガーに入ったら移動処理を開始
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // プレイヤーにアタッチされている MoveControl を取得
            MoveControl moveControl = other.GetComponent<MoveControl>();
            if (moveControl == null) return;

            // コルーチンで「returnPosition へ移動する」処理を実行
            StartCoroutine(MovePlayerToReturnPoint(moveControl));
        }

        /// <summary>
        /// プレイヤーを returnPosition に移動させるコルーチン
        /// </summary>
        private IEnumerator MovePlayerToReturnPoint(MoveControl moveControl)
        {
            float stopDistance = 0.1f; // この距離以内であれば停止とみなす

            while (true)
            {
                Vector3 playerPos = moveControl.transform.position;
                Vector3 targetPos = returnPosition.position;

                // 水平移動だけにしたい場合は高さを維持
                targetPos.y = playerPos.y;

                // 現在位置と目標位置の差分
                Vector3 diff = targetPos - playerPos;
                // 一定距離未満になったら移動終了
                if (diff.sqrMagnitude <= stopDistance * stopDistance)
                    break;

                // 正規化して方向ベクトルを得る
                diff.Normalize();

                // --- カメラの Y 軸回転を逆変換して、MoveControl への入力に合わせる ---
                Transform cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogWarning("Camera.main が見つかりません。");
                    yield break;
                }

                // カメラの水平回転（Y 軸のみ）
                Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

                // MoveControl 内では "cameraYawRotation * (leftStickInput.normalized)" するため、
                // 逆変換して "leftStickInput" を求める
                Vector3 leftStickInput3D = Quaternion.Inverse(cameraYawRotation) * diff;
                Vector2 leftStickInput = new Vector2(leftStickInput3D.x, leftStickInput3D.z);

                // 必要に応じて WASD 相当に丸めたい場合はここで処理（省略可）
                leftStickInput = SnapToKeyboardDirections(leftStickInput, 0.5f);

                // MoveControl に入力を送る
                moveControl.Move(leftStickInput);

                yield return null;
            }

            // 目標地点に到着したら入力をクリアして停止
            moveControl.Move(Vector2.zero);
        }

        // 必要に応じて、WASD 相当へスナップするメソッドを追加する場合の例
        private Vector2 SnapToKeyboardDirections(Vector2 input, float threshold)
        {
            float x = 0f;
            float y = 0f;
            if (Mathf.Abs(input.x) >= threshold) x = Mathf.Sign(input.x);
            if (Mathf.Abs(input.y) >= threshold) y = Mathf.Sign(input.y);
            return new Vector2(x, y);
        }
    }
}
