using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using VContainer;  // VContainer を使って Inject する場合に必要
using AnoGame.Application.Input; // IInputActionProvider の名前空間を解決

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Scrollbar 操作を“UI”アクションマップ経由で行いたい場合のサンプル
    /// </summary>
    public class ScrollbarController : MonoBehaviour
    {
        //──────────────────────────────────────────
        // ① IInputActionProvider を Inject して受け取る
        //──────────────────────────────────────────
        [Inject] private IInputActionProvider _inputProvider;

        //──────────────────────────────────────────
        // ② もし UIManager が必要であればそのまま SerializeField で持っておく
        //──────────────────────────────────────────
        [SerializeField] private UIManager uiManager;

        //──────────────────────────────────────────
        // ③ UI マップから取得する各 InputAction を宣言
        //──────────────────────────────────────────
        private InputAction selectAction;
        private InputAction confirmAction;
        private InputAction cancelAction;

        //──────────────────────────────────────────
        // ④ 実際に操作対象の Scrollbar を保持
        //──────────────────────────────────────────
        private Scrollbar targetScrollbar;

        private void Awake()
        {
            //──────────────────────────────────────────
            // A) UI 用の ActionMap に切り替える
            //──────────────────────────────────────────
            _inputProvider.SwitchToUI();

            //──────────────────────────────────────────
            // B) UI の ActionMap を取得しておく
            //──────────────────────────────────────────
            var uiMap = _inputProvider.GetUIActionMap();

            //──────────────────────────────────────────
            // C) “Select” アクションを取得し、performed コールバックを張る
            //──────────────────────────────────────────
            selectAction = uiMap.FindAction("Select", throwIfNotFound: true);
            selectAction.performed += OnSelectPerformed;

            //──────────────────────────────────────────
            // D) “Confirm” アクション (必要があれば）
            //──────────────────────────────────────────
            confirmAction = uiMap.FindAction("Confirm", throwIfNotFound: true);
            confirmAction.performed += OnConfirmPerformed;

            //──────────────────────────────────────────
            // E) “Cancel” アクション
            //──────────────────────────────────────────
            cancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);
            cancelAction.performed += OnCancelPerformed;
        }

        private void OnDestroy()
        {
            //──────────────────────────────────────────
            // Awake で登録したコールバックは必ず外す
            //──────────────────────────────────────────
            if (selectAction != null)
                selectAction.performed -= OnSelectPerformed;
            if (confirmAction != null)
                confirmAction.performed -= OnConfirmPerformed;
            if (cancelAction != null)
                cancelAction.performed -= OnCancelPerformed;
        }

        /// <summary>
        /// UIManager などから呼ばれて、実際に操作したい Scrollbar をセットする
        /// </summary>
        public void SetTarget(Scrollbar sb)
        {
            targetScrollbar = sb;
        }

        /// <summary>
        /// “Select” 入力時 (左右スティックなど) に呼ばれる処理
        /// </summary>
        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            if (targetScrollbar == null) return;

            Vector2 input = context.ReadValue<Vector2>();
            // 左入力なら value を -0.1 だけ減らす
            if (input.x < -0.5f)
            {
                float newVal = Mathf.Clamp01(targetScrollbar.value - 0.1f);
                targetScrollbar.value = newVal;
            }
            // 右入力なら value を +0.1 だけ増やす
            else if (input.x > 0.5f)
            {
                float newVal = Mathf.Clamp01(targetScrollbar.value + 0.1f);
                targetScrollbar.value = newVal;
            }
        }

        /// <summary>
        /// “Confirm” 入力時 (必要なら)
        /// </summary>
        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            // 例: 確定ボタンでスクロール操作モードを解除したいなら
            // uiManager.CloseScrollbarMode();
        }

        /// <summary>
        /// “Cancel” 入力時
        /// </summary>
        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            // 例: キャンセルでスクロール操作モードを解除
            // uiManager.CloseScrollbarMode();
        }
    }
}
