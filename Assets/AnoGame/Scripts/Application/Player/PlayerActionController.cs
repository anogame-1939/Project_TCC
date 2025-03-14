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

       private System.Collections.IEnumerator DisableInputRoutine(float duration)
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

        /// <summary>
        /// ターゲットの方向を向く
        /// </summary>
        /// <param name="target"></param>
        public void FaceTarget(GameObject target)
        {
            if (target == null)
                return;
            StartCoroutine(FaceTargetRoutine(target));
        }

        private IEnumerator FaceTargetRoutine(GameObject target)
        {
            // プレイヤーとターゲットの水平な位置を取得
            Vector3 playerPosition = transform.position;
            Vector3 targetPosition = target.transform.position;

            // Y軸は無視して水平な方向ベクトルを計算
            Vector3 desiredDirection = targetPosition - playerPosition;
            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
                yield break;
            
            desiredDirection.Normalize();

            // カメラのY軸回転を取得（見下ろし視点でも、カメラのY軸は有効と仮定）
            Transform cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("Camera.mainが見つかりません。");
                yield break;
            }
            // カメラのY軸回転（水平回転）のみを抽出
            Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

            // MoveControl 内部では
            //   _moveDirection = cameraYawRotation * (leftStickInput.normalized)
            // となっているため、desiredDirection になるようにするには
            //   leftStickInput = Quaternion.Inverse(cameraYawRotation) * desiredDirection
            Vector3 leftStickInput3D = Quaternion.Inverse(cameraYawRotation) * desiredDirection;
            Vector2 leftStickInput = new Vector2(leftStickInput3D.x, leftStickInput3D.z);

            // --- ここで “WASD 相当” に丸める ---
            Vector2 snappedInput = SnapToKeyboardDirections(leftStickInput, 0.5f);

            // 向き更新用に一時的に入力を送る
            moveControl.Move(snappedInput);

            Debug.Log($"FaceTargetRoutine: targetPosition={targetPosition}");
            Debug.Log($"FaceTargetRoutine: leftStickInput(before snap)={leftStickInput}, snappedInput={snappedInput}");

            // 1フレーム待ってから入力をクリア（必要に応じて調整）
            yield return null;
            moveControl.Move(Vector2.zero);
        }

        /// <summary>
        /// アナログ入力ベクトルを 8方向(上下左右＋斜め)にスナップ(0/1化)するヘルパーメソッド
        /// </summary>
        /// <param name="input">アナログ入力</param>
        /// <param name="threshold">しきい値。例:0.5f</param>
        /// <returns>上下左右斜めいずれかの(-1,0,1)成分を持つ Vector2</returns>
        private Vector2 SnapToKeyboardDirections(Vector2 input, float threshold)
        {
            float x = 0f;
            float y = 0f;

            // X成分が threshold を超えたら ±1、それ以下なら 0
            if (Mathf.Abs(input.x) >= threshold)
            {
                x = Mathf.Sign(input.x);
            }

            // Y成分が threshold を超えたら ±1、それ以下なら 0
            if (Mathf.Abs(input.y) >= threshold)
            {
                y = Mathf.Sign(input.y);
            }

            // これで上下左右・斜めのいずれか(8方向)になる
            return new Vector2(x, y);
        }

   }
}