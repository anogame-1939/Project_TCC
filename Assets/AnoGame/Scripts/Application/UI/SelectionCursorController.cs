using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Events;

namespace AnoGame.Application.UI
{
    [RequireComponent(typeof(PlayerInput))]
    public class SelectionCursorController : MonoBehaviour
    {
        [Header("=== Events ===")]
        [SerializeField] private UnityEvent onSelect;
        [SerializeField] private UnityEvent onConfirm;
        [SerializeField] private UnityEvent onCancel;

        private PlayerInput playerInput;
        private InputAction selectAction;
        private InputAction confirmAction;
        private InputAction cancelAction;

        // ★ 現在アクティブな UISection (UIManager からセットしてもらう)
        private UISection currentSection;

        // カーソルのイメージ (UISectionのオフセットを適用するため保持)
        [SerializeField] private Image cursorImage;

        // 現在のインデックス
        private int currentIndex = 0;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerInput.SwitchCurrentActionMap("UI");

            // “Select” アクションを取得
            selectAction = playerInput.actions.FindAction("Select", throwIfNotFound: true);
            selectAction.started += OnSelectStarted;

            // “Confirm” アクションを取得
            confirmAction = playerInput.actions.FindAction("Confirm", throwIfNotFound: true);
            confirmAction.performed += OnConfirmPerformed;

            // “Cancel” アクションを取得
            cancelAction = playerInput.actions.FindAction("Cancel", throwIfNotFound: true);
            cancelAction.performed += OnCancelPerformed;
        }

        private void OnDestroy()
        {
            if (selectAction != null)
            {
                selectAction.started -= OnSelectStarted;
            }
            if (confirmAction != null)
            {
                confirmAction.performed -= OnConfirmPerformed;
            }
            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancelPerformed;
            }
        }

        /// <summary>
        /// UIManager から現在の UISection を設定してもらう
        /// </summary>
        public void SetUISection(UISection section)
        {
            currentSection = section;
            // リストを反映
            currentIndex = Mathf.Clamp(currentSection.lastIndex, 0, currentSection.selectables.Count - 1);

            UpdateSelection();
        }

        /// <summary>
        /// 現在のインデックスを取得（UIManagerなどが参照する）
        /// </summary>
        public int GetCurrentIndex()
        {
            return currentIndex;
        }

        //========================
        // Selectアクションの処理
        //========================

        private void OnSelectStarted(InputAction.CallbackContext context)
        {
            onSelect?.Invoke();

            if (currentSection == null || currentSection.selectables.Count == 0) return;

            Vector2 inputValue = context.ReadValue<Vector2>();
            if (inputValue.y > 0)
            {
                MoveSelectionUp();
            }
            else if (inputValue.y < 0)
            {
                MoveSelectionDown();
            }
        }

        //========================
        // Confirmアクションの処理
        //========================

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            onConfirm?.Invoke();

            if (currentSection == null || currentSection.selectables.Count == 0) return;

            Debug.Log("Confirm pressed! Selected: " + currentSection.selectables[currentIndex].gameObject.name);
            var button = currentSection.selectables[currentIndex].GetComponent<Button>();
            if (button != null)
            {
                button.onClick.Invoke();
            }
        }

        //========================
        // Cancelアクションの処理
        //========================

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            onCancel?.Invoke();

            Debug.Log("Cancel pressed!");

            // ★ 現在のセクションに onCancel イベントがあれば呼ぶ
            if (currentSection != null && currentSection.onCancel != null)
            {
                currentSection.onCancel.Invoke();
            }
        }

        //========================
        // 選択移動関連
        //========================

        private void MoveSelectionUp()
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = currentSection.selectables.Count - 1;
            }
            UpdateSelection();
        }

        private void MoveSelectionDown()
        {
            currentIndex++;
            if (currentIndex >= currentSection.selectables.Count)
            {
                currentIndex = 0;
            }
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (cursorImage != null && currentSection != null && currentSection.selectables.Count > 0)
            {
                var target = currentSection.selectables[currentIndex];
                // UISection が持つカーソルオフセットを適用
                Vector2 offset = currentSection.cursorOffset;
                cursorImage.transform.position 
                    = target.transform.position + (Vector3)offset;

                Debug.Log("Selected Button: " + target.gameObject.name);
            }
        }
    }
}
