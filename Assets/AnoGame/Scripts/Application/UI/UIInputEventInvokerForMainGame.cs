using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace AnoGame.Application.UI
{
    public class UIInputEventInvokerForMainGame : MonoBehaviour
    {
        // InputSystemUIInputModuleの参照をInspectorから設定
        [SerializeField] private InputSystemUIInputModule inputModule;

        // 各アクションに対応するUnityEvent
        [SerializeField] private UnityEvent onMove;
        [SerializeField] private UnityEvent onSubmit;
        [SerializeField] private UnityEvent onCancel;
        [SerializeField] private UnityEvent onLeftClick;

        private void OnEnable()
        {
            if (inputModule != null)
            {
                // Moveアクションの開始時にイベントを購読
                if (inputModule.move != null && inputModule.move.action != null)
                {
                    inputModule.move.action.started += OnMove;
                }
                // Submitアクションの開始時にイベントを購読
                if (inputModule.submit != null && inputModule.submit.action != null)
                {
                    inputModule.submit.action.started += OnSubmit;
                }
                // Cancelアクションの開始時にイベントを購読
                if (inputModule.cancel != null && inputModule.cancel.action != null)
                {
                    inputModule.cancel.action.started += OnCancel;
                }
                // Left Clickアクションの開始時にイベントを購読
                if (inputModule.leftClick != null && inputModule.leftClick.action != null)
                {
                    inputModule.leftClick.action.started += OnLeftClick;
                }
            }
        }

        private void OnDisable()
        {
            if (inputModule != null)
            {
                // 購読解除（OnEnableで登録したイベントの解除）
                if (inputModule.move != null && inputModule.move.action != null)
                {
                    inputModule.move.action.started -= OnMove;
                }
                if (inputModule.submit != null && inputModule.submit.action != null)
                {
                    inputModule.submit.action.started -= OnSubmit;
                }
                if (inputModule.cancel != null && inputModule.cancel.action != null)
                {
                    inputModule.cancel.action.started -= OnCancel;
                }
                if (inputModule.leftClick != null && inputModule.leftClick.action != null)
                {
                    inputModule.leftClick.action.started -= OnLeftClick;
                }
            }
        }

        // Moveアクション開始時のコールバック
        private void OnMove(InputAction.CallbackContext context)
        {
            Debug.Log($"Move action triggered:{GameStateManager.Instance.CurrentState}");
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                return;
            }
            onMove?.Invoke();
        }

        // Submitアクション開始時のコールバック
        private void OnSubmit(InputAction.CallbackContext context)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                return;
            }
            onSubmit?.Invoke();
        }

        // Cancelアクション開始時のコールバック
        private void OnCancel(InputAction.CallbackContext context)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                return;
            }
            onCancel?.Invoke();
        }

        // Left Clickアクション開始時のコールバック
        private void OnLeftClick(InputAction.CallbackContext context)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                return;
            }
            onLeftClick?.Invoke();
        }
    }
}