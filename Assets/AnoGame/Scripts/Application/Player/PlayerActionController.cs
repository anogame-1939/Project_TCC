using UnityEngine;
using UnityEngine.InputSystem;
using Unity.TinyCharacterController.Control;
using System.Collections;

namespace AnoGame.Application.Player.Control
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerActionController : MonoBehaviour
    {
        [SerializeField] private MoveControl moveControl;
        private bool isInputEnabled = true;
        private InputAction moveAction;
        private bool isKeyHeld = false;

        private void Awake()
        {
            moveControl = GetComponent<MoveControl>();
            var playerInput = GetComponent<PlayerInput>();
            moveAction = playerInput.actions["Move"];

            // キーの押下状態管理用のイベント登録
            moveAction.started += OnMoveStarted;
            moveAction.canceled += OnMoveCanceled;
        }

        private void OnDestroy()
        {
            if (moveAction != null)
            {
                moveAction.started -= OnMoveStarted;
                moveAction.canceled -= OnMoveCanceled;
            }
        }

        private void FixedUpdate()
        {
            // キーが押されている場合のみ入力値を取得して反映
            if (isKeyHeld && isInputEnabled)
            {
                Vector2 inputValue = moveAction.ReadValue<Vector2>();
                moveControl.Move(inputValue);
            }
        }

        // キー押下開始時
        private void OnMoveStarted(InputAction.CallbackContext context)
        {
            isKeyHeld = true;
        }

        // キー解放時
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            isKeyHeld = false;
            moveControl.Move(Vector2.zero);
        }

        public void DisableInput(float duration)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DisableInputRoutine(duration));
            }
        }

        private IEnumerator DisableInputRoutine(float duration)
        {
            SetInputEnabled(false);
            yield return new WaitForSeconds(duration);
            SetInputEnabled(true);
        }

        public bool IsInputEnabled => isInputEnabled;

        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;
            if (!enabled)
            {
                moveControl.Move(Vector2.zero);
            }
        }

        //============================================================
        // ここからが本題：ターゲットへ移動しつつ、オプションでバックステップも挟む
        //============================================================

        /// <summary>
        /// 指定した位置へ移動する（オプションでバックステップを先に行う）
        /// </summary>
        /// <param name="target">移動先となるターゲット(GameObject)</param>
        /// <param name="doBackstep">最初にバックステップを行うかどうか</param>
        public void MoveToTarget(GameObject target, bool doBackstep = false)
        {
            if (target == null) return;
            StartCoroutine(MoveToTargetRoutine(target, doBackstep));
        }

        private IEnumerator MoveToTargetRoutine(GameObject target, bool doBackstep)
        {
            // もし先にバックステップを行いたい場合
            if (doBackstep)
            {
                // MoveControl 側に用意された Backstep メソッドを呼び出す
                moveControl.Backstep(speed: 5f, duration: 0.3f);

                // バックステップが終わるまで待機
                yield return new WaitForSeconds(0.3f);
            }

            // プレイヤーとターゲットの位置を取得
            Vector3 playerPos = transform.position;
            Vector3 targetPos = target.transform.position;

            // しきい値(目的地までの誤差)を決める
            float stopDistance = 0.1f;

            // ターゲットに近づくまでループ
            while (true)
            {
                // 毎フレーム、プレイヤーとターゲットの位置を更新
                playerPos = transform.position;
                targetPos = target.transform.position;

                // ターゲットとの水平距離が十分小さければ移動終了
                Vector3 diff = targetPos - playerPos;
                diff.y = 0f;
                if (diff.sqrMagnitude <= stopDistance * stopDistance)
                    break;

                // カメラの Y 軸回転を取得
                Transform cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogWarning("Camera.main が見つかりません。");
                    yield break;
                }
                Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

                // 移動先への方向を算出
                diff.Normalize();

                // MoveControl 内では cameraYawRotation * leftStickInput.normalized するので
                // 逆変換して leftStickInput を求める
                Vector3 leftStickInput3D = Quaternion.Inverse(cameraYawRotation) * diff;
                Vector2 leftStickInput = new Vector2(leftStickInput3D.x, leftStickInput3D.z);

                // 必要に応じて WASD 相当へスナップ
                Vector2 snappedInput = SnapToKeyboardDirections(leftStickInput, 0.5f);

                // MoveControl に入力を送る
                moveControl.Move(snappedInput);

                // 次のフレームまで待機
                yield return null;
            }

            // 目標地点に到着したので入力をクリアして終了
            moveControl.Move(Vector2.zero);
        }

        /// <summary>
        /// アナログ入力ベクトルを 8方向(上下左右＋斜め)にスナップ(0/1化)するヘルパーメソッド
        /// </summary>
        private Vector2 SnapToKeyboardDirections(Vector2 input, float threshold)
        {
            float x = 0f;
            float y = 0f;

            if (Mathf.Abs(input.x) >= threshold)
            {
                x = Mathf.Sign(input.x);
            }
            if (Mathf.Abs(input.y) >= threshold)
            {
                y = Mathf.Sign(input.y);
            }

            return new Vector2(x, y);
        }
    }
}
