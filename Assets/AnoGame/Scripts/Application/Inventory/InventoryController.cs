using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using AnoGame.Application.Input;  // IInputActionProvider の名前空間

namespace AnoGame.Application.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField]
        private InventoryViewer _inventoryViewer;

        private CanvasGroup _canvasGroup;
        private InventoryManager _inventoryManager;

        // Player マップの Inventory 開閉用アクション
        private InputAction _inventoryAction;

        //──────────────────────────────────────────
        // IInputActionProvider を Inject で受け取る
        //──────────────────────────────────────────
        [Inject]
        private IInputActionProvider _inputProvider;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
        }

        void Start()
        {
            if (_inventoryViewer == null)
            {
                Debug.LogError("[InventoryController] _inventoryViewer が設定されていません。");
                return;
            }

            // CanvasGroup をキャッシュ
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("[InventoryController] CanvasGroup がアタッチされていません。");
                return;
            }

            // Player マップへ切り替え
            _inputProvider.SwitchToPlayer();
            var playerMap = _inputProvider.GetPlayerActionMap();
            playerMap.Enable();

            // Player マップから Inventory アクションを取得して購読
            _inventoryAction = playerMap.FindAction("Inventory", throwIfNotFound: true);
            _inventoryAction.performed += OnInventoryPerformed;

            // 初期状態は非表示
            Hide();
        }

        void OnDestroy()
        {
            if (_inventoryAction != null)
                _inventoryAction.performed -= OnInventoryPerformed;
        }

        private void OnInventoryPerformed(InputAction.CallbackContext ctx)
        {
            ToggleInventory();
        }

        void ToggleInventory()
        {
            var currentState = GameStateManager.Instance.CurrentState;
            if (currentState == GameState.Gameplay)
            {
                GameStateManager.Instance.SetState(GameState.Inventory);
                Show();
            }
            else if (currentState == GameState.Inventory)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
                Hide();
            }
        }

        public void Show()
        {
            // 在庫データ更新
            var items = _inventoryManager.GetInventory();
            if (items != null)
            {
                var inventory = new Domain.Data.Models.Inventory();
                foreach (var item in items)
                    inventory.AddItem(item);
                _inventoryViewer.UpdateInventory(inventory);
            }

            // 表示＆カーソル解放
            _canvasGroup.alpha      = 1;
            Cursor.lockState        = CursorLockMode.None;
            Cursor.visible          = true;

            // ※ここで UI マップ参照（GetUIActionMap）はまだ行いません
        }

        public void Hide()
        {
            // 非表示＆カーソルロック
            _canvasGroup.alpha      = 0;
            Cursor.lockState        = CursorLockMode.Locked;
            Cursor.visible          = false;

            StartCoroutine(EnforceCursorHide());
        }

        public void Close()
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
            Hide();
        }

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
