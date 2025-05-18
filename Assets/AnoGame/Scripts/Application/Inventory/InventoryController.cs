using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace AnoGame.Application.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField]
        InputActionAsset _inputActionAsset;

        [SerializeField]
        InventoryViewer _inventoryViewer;

        private CanvasGroup _canvasGroup;
        private InventoryManager _inventoryManager;
        private InputAction _inventoryAction;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
        }
        
        void Start()
        {
            if (_inputActionAsset == null)
            {
                Debug.LogError("_inputActionAssetが設定されていません。");
                return;
            }
            if (_inventoryViewer == null)
            {
                Debug.LogError("_inventoryViewerが設定されていません。");
                return;
            }

            var actionMap = _inputActionAsset.FindActionMap("Player");
            actionMap.Enable();

            // Inventoryアクションを取得して購読
            _inventoryAction = actionMap.FindAction("Inventory");
            _inventoryAction.performed += OnInventoryPerformed;

            _canvasGroup = GetComponent<CanvasGroup>();

            // 初期状態は非表示
            Hide();
        }

        private void OnInventoryPerformed(InputAction.CallbackContext ctx)
        {
            ToggleInventory();
        }

        void OnDestroy()
        {
            // 購読解除
            if (_inventoryAction != null)
            {
                _inventoryAction.performed -= OnInventoryPerformed;
            }
        }

        /// <summary>
        /// Inventoryの表示／非表示をグローバル状態に応じて切り替えます。
        /// </summary>
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
            var inventoryItems = _inventoryManager.GetInventory();
            if (inventoryItems != null)
            {
                var inventory = new Domain.Data.Models.Inventory();
                foreach (var item in inventoryItems)
                {
                    inventory.AddItem(item);
                }
                _inventoryViewer.UpdateInventory(inventory);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _canvasGroup.alpha = 0;

            StartCoroutine(EnforceCursorHide());
        }

        private IEnumerator EnforceCursorHide()
        {
            yield return new WaitForSeconds(5f);
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
