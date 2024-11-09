using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Data;
using AnoGame.Application;

namespace AnoGame.Application.Inventory
{
    public class InventoryViewer : MonoBehaviour
    {
        [SerializeField]
        InputActionAsset _inputActionAsset;

        [SerializeField]
        List<InventoryItem> _inventoryItem;

        [SerializeField]
        List<InventoryItemUI> _inventoryItemUI;

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
            GetComponent<CanvasGroup>().alpha = 1;
        }

        public void Hide()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            GetComponent<CanvasGroup>().alpha = 0;
        }
    }
}