using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using AnoGame.Application.Input;

namespace AnoGame.Application.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField]
        private InventoryViewer _inventoryViewer;

        private CanvasGroup _canvasGroup;
        private InventoryManager _inventoryManager;

        // Player マップの Inventory 開閉用
        private InputAction _inventoryOpenAction;
        // UI マップの Cancel（閉じる用）
        private InputAction _cancelAction;
        // UI マップの Inventory（閉じる用）
        private InputAction _inventoryCloseAction;

        [Inject] private IInputActionProvider _inputProvider;
        [Inject] public void Construct(InventoryManager inventoryManager) => _inventoryManager = inventoryManager;

        void Start()
        {
            if (_inventoryViewer == null)
            {
                Debug.LogError("[InventoryController] _inventoryViewer が設定されていません。");
                return;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("[InventoryController] CanvasGroup がアタッチされていません。");
                return;
            }

            // Player マップ → Inventory 開閉
            _inputProvider.SwitchToPlayer();
            var playerMap = _inputProvider.GetPlayerActionMap();
            playerMap.Enable();
            _inventoryOpenAction = playerMap.FindAction("Inventory", throwIfNotFound: true);
            _inventoryOpenAction.performed += OnInventoryOpenPerformed;

            Hide();
        }

        void OnDestroy()
        {
            // Player マップ購読解除
            if (_inventoryOpenAction != null)
                _inventoryOpenAction.performed -= OnInventoryOpenPerformed;
            // UI マップ購読解除
            if (_cancelAction != null)
                _cancelAction.performed  -= OnCancelPerformed;
            if (_inventoryCloseAction != null)
                _inventoryCloseAction.performed -= OnCancelPerformed;
        }

        private void OnInventoryOpenPerformed(InputAction.CallbackContext ctx)
            => ToggleInventory();

        void ToggleInventory()
        {
            var state = GameStateManager.Instance.CurrentState;
            if (state == GameState.Gameplay)
            {
                GameStateManager.Instance.SetState(GameState.Inventory);
                Show();
            }
            else if (state == GameState.Inventory)
            {
                Close();
            }
        }

        public void Show()
        {
            // データ更新
            var items = _inventoryManager.GetInventory();
            if (items != null)
            {
                var inv = new Domain.Data.Models.Inventory();
                foreach (var item in items)
                    inv.AddItem(item);
                _inventoryViewer.UpdateInventory(inv);
            }

            // UI マップへ切り替え ＆ Cancel / Inventory（UI） を購読
            // _inputProvider.SwitchToUI();
            var uiMap = _inputProvider.GetUIActionMap();

            _cancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);
            _cancelAction.performed += OnCancelPerformed;

            _inventoryCloseAction = uiMap.FindAction("Inventory", throwIfNotFound: true);
            if (_inventoryCloseAction != null)
                _inventoryCloseAction.performed += OnCancelPerformed;

            // 表示＆カーソル解放
            _canvasGroup.alpha      = 1;
            Cursor.lockState        = CursorLockMode.None;
            Cursor.visible          = true;
        }

        public void Hide()
        {
            // 非表示＆カーソルロック
            _canvasGroup.alpha      = 0;
            Cursor.lockState        = CursorLockMode.Locked;
            Cursor.visible          = false;

            // UI マップ購読解除
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancelPerformed;
                _cancelAction = null;
            }
            if (_inventoryCloseAction != null)
            {
                _inventoryCloseAction.performed -= OnCancelPerformed;
                _inventoryCloseAction = null;
            }

            // Player マップへ復帰
            _inputProvider.SwitchToPlayer();

            StartCoroutine(EnforceCursorHide());
        }

        public void Close()
        {
            Debug.Log("Close");
            GameStateManager.Instance.SetState(GameState.Gameplay);
            Hide();
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
            => Close();

        private IEnumerator EnforceCursorHide()
        {
            yield return new WaitForSeconds(5f);
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible   = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible   = true;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
        }
    }
}
