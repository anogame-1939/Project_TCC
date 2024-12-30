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

        private bool isInventoryMode = true;
        private CanvasGroup _canvasGroup;
        private InventoryManager _inventoryManager;

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
                Debug.LogError("_inventoryManagerが設定されていません。");
                return;
            }

            var actionMap = _inputActionAsset.FindActionMap("Player");
            actionMap.Enable();

            var inventory = actionMap.FindAction("Inventory");
            inventory.performed += ctx =>
            {
                ToggleCursorLock();
            };
            isInventoryMode = false;
            _canvasGroup = GetComponent<CanvasGroup>();
            Hide();
        }

        void ToggleCursorLock()
        {
            isInventoryMode = !isInventoryMode;

            if (isInventoryMode)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        public void Show()
        {
            // 最新のインベントリ情報を取得して表示を更新
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
        }
    }
}