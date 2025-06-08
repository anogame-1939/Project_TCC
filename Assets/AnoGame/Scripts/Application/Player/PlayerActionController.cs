using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.TinyCharacterController.Control;
using System.Collections;
using VContainer;
using AnoGame.Application.Input;  // IInputActionProvider の名前空間

namespace AnoGame.Application.Player.Control
{
    /// <summary>
    /// 強制移動の開始・終了を受け取るためのインターフェース
    /// </summary>
    public interface IForcedMoveController
    {
        void StopChasing();
        void StartChasing();
    }

    [AddComponentMenu("Player/" + nameof(PlayerActionController))]
    public class PlayerActionController : MonoBehaviour, IForcedMoveController
    {
        [SerializeField] private MoveControl moveControl;
        [SerializeField] private Animator animator;

        // MoveControlの現在速度がこれ以上のとき IsMove を true にする
        [SerializeField] private float moveSpeedThreshold = 0.5f;

        //──────────────────────────────────────────
        // ① IInputActionProvider を Inject で受け取る
        //──────────────────────────────────────────
        [Inject] private IInputActionProvider _inputProvider;

        private bool isInputEnabled = true;
        private InputAction moveAction;
        private bool isKeyHeld = false;

        private void Awake()
        {
            //──────────────────────────────────────────
            // MoveControl / Animator が未設定なら GetComponent で取得
            //──────────────────────────────────────────
            if (moveControl == null)
            {
                moveControl = GetComponent<MoveControl>();
                if (moveControl == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerActionController)}] MoveControl がアタッチされていません。");
                }
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerActionController)}] Animator がアタッチされていません。");
                }
            }

            //──────────────────────────────────────────
            // ② IInputActionProvider 経由で Player マップを有効化し、
            //     “Move” アクションを取得してキャッシュ
            //──────────────────────────────────────────
            // _inputProvider.SwitchToPlayer();
            var playerMap = _inputProvider.GetPlayerActionMap();
            moveAction = playerMap.FindAction("Move", throwIfNotFound: true);

            //──────────────────────────────────────────
            // ③ キーの押下状態管理用イベントを登録
            //──────────────────────────────────────────
            moveAction.started  += OnMoveStarted;
            moveAction.canceled += OnMoveCanceled;
        }

        private void OnDestroy()
        {
            if (moveAction != null)
            {
                moveAction.started  -= OnMoveStarted;
                moveAction.canceled -= OnMoveCanceled;
            }
        }

        private void Update()
        {
            // MoveControlの速度を取得し、閾値を超えていれば移動アニメをONに
            if (animator != null && moveControl != null)
            {
                float currentSpeed = moveControl.CurrentSpeed;
                bool isMove = currentSpeed > moveSpeedThreshold;
                animator.SetBool("IsMove", isMove);
            }
        }

        private void FixedUpdate()
        {
            // ゲームがプレイ中でなければ入力を無視
            if (GameStateManager.Instance.CurrentState != GameState.Gameplay)
            {
                return;
            }

            // キーが押されており、かつ入力が有効であれば MoveControl に入力値を渡す
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

        /// <summary>
        /// 一定時間入力を無効化する
        /// </summary>
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

        /// <summary>
        /// ターゲットの方向を向く
        /// </summary>
        public void FaceTarget(GameObject target)
        {
            if (target == null) return;
            StartCoroutine(FaceTargetRoutine(target));
        }

        private IEnumerator FaceTargetRoutine(GameObject target)
        {
            // プレイヤーとターゲットの水平な位置を計算
            Vector3 playerPosition = transform.position;
            Vector3 targetPosition = target.transform.position;
            Vector3 desiredDirection = targetPosition - playerPosition;
            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
                yield break;

            desiredDirection.Normalize();

            // カメラのY軸回転を取得
            Transform cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("Camera.main が見つかりません。");
                yield break;
            }
            Quaternion cameraYawRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

            // MoveControl 内では leftStickInput をカメラ回転で変換する 前提
            Vector3 leftStickInput3D = Quaternion.Inverse(cameraYawRotation) * desiredDirection;
            Vector2 leftStickInput = new Vector2(leftStickInput3D.x, leftStickInput3D.z);

            // 8方向スナップを行う
            Vector2 snappedInput = SnapToKeyboardDirections(leftStickInput, 0.5f);

            // 一時的に向かせる入力を送信
            moveControl.Move(snappedInput);

            // 少し待ってから入力をクリア
            yield return new WaitForSeconds(0.1f);
            moveControl.Move(Vector2.zero);
        }

        /// <summary>
        /// アナログ入力を8方向(上下左右＋斜め)にスナップするヘルパーメソッド
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

        //========================================================
        // ↓↓↓ 強制移動(ForecedTransformMover)用の有効/無効メソッド ↓↓↓
        //========================================================

        /// <summary>
        /// 強制移動を始める前に呼び出して、このコンポーネントを無効化する
        /// </summary>
        public void StopChasing()
        {
            // スクリプト全体を無効化
            this.enabled = false;
            // あるいは入力だけ無効化する場合:
            // SetInputEnabled(false);
        }

        /// <summary>
        /// 強制移動が完了したら呼び出して、このコンポーネントを再有効化する
        /// </summary>
        public void StartChasing()
        {
            this.enabled = true;
            // あるいは入力だけ再有効化する場合:
            // SetInputEnabled(true);
        }

        /// <summary>
        /// ダミー入力を送りつつ1フレーム待機し、入力をクリアする例
        /// </summary>
        public IEnumerator InstantMoveUpdate(Vector2 moveInput)
        {
            moveControl.Move(moveInput);
            yield return new WaitForSeconds(2f);
            moveControl.Move(Vector2.zero);
        }
    }
}
