using System;
using System.Collections.Generic;
using AnoGame.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnoGame.Inventory
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
        InventoryManager _inventoryManager;

        private bool isInventoryMode = true;
        
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
            if (_inventoryManager == null)
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
            Hide();
        }

        private void UpdateInventoryItemUI(GameData data)
        {
            _inventoryItem = data.inventory;


            _inventoryManager.UpdateInventory(_inventoryItem);

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
            _inventoryManager.GetComponent<CanvasGroup>().alpha = 1;
        }

        public void Hide()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _inventoryManager.GetComponent<CanvasGroup>().alpha = 0;
        }
    }
}