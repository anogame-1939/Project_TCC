using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace AnoGame.Application.UI
{
    public class ScrollbarController : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private UIManager uiManager;

        private InputAction selectAction;
        private InputAction confirmAction;
        private InputAction cancelAction;

        private Scrollbar targetScrollbar;

        private void Awake()
        {
            if (!playerInput) playerInput = GetComponent<PlayerInput>();

            // "UI" アクションマップを使う場合
            playerInput.SwitchCurrentActionMap("UI");

            // Selectアクションで左右入力を取得
            selectAction = playerInput.actions.FindAction("Select", throwIfNotFound: true);
            selectAction.performed += OnSelectPerformed;

            // Confirmアクション (必要なら)
            confirmAction = playerInput.actions.FindAction("Confirm", throwIfNotFound: true);
            confirmAction.performed += OnConfirmPerformed;

            // Cancelアクション
            cancelAction = playerInput.actions.FindAction("Cancel", throwIfNotFound: true);
            cancelAction.performed += OnCancelPerformed;
        }

        private void OnDestroy()
        {
            if (selectAction != null)
                selectAction.performed -= OnSelectPerformed;
            if (confirmAction != null)
                confirmAction.performed -= OnConfirmPerformed;
            if (cancelAction != null)
                cancelAction.performed -= OnCancelPerformed;
        }

        // UIManager から呼ばれる
        public void SetTarget(Scrollbar sb)
        {
            targetScrollbar = sb;
        }

        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            if (targetScrollbar == null) return;

            Vector2 input = context.ReadValue<Vector2>();
            // 左入力
            if (input.x < -0.5f)
            {
                float newVal = Mathf.Clamp01(targetScrollbar.value - 0.1f);
                targetScrollbar.value = newVal;
            }
            // 右入力
            else if (input.x > 0.5f)
            {
                float newVal = Mathf.Clamp01(targetScrollbar.value + 0.1f);
                targetScrollbar.value = newVal;
            }
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            // 必要に応じて "確定" 処理を行う or 何もしない
            // 例: confirm でもモードを抜けるなら
            // uiManager.CloseScrollbarMode();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            // キャンセルでスクロールバー操作モードを終了
            // uiManager.CloseScrollbarMode();
        }
    }
}