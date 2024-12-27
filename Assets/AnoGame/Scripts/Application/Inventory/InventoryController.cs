using System.Collections    ;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Data;

namespace AnoGame.Application.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField]
        InputActionAsset _inputActionAsset;

        [SerializeField]
        List<InventoryItem> _inventoryItem;

        [SerializeField]
        List<InventoryItemUI> _inventoryItemUI;

        [SerializeField]
        InventoryViewer _inventoryViewer;

        private bool isInventoryMode = true;
        private CanvasGroup _canvasGroup;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            GameManager.Instance.SaveGameData += UpdateInventoryItemUI;
            GameManager.Instance.LoadGameData += UpdateInventoryItemUI;

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

        private void UpdateInventoryItemUI(GameData data)
        {
            _inventoryItem = data.inventory;
            _inventoryViewer.UpdateInventory(_inventoryItem);
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