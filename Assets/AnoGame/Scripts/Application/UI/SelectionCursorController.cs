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
        [Header("UI上で選択可能なボタンなどのリスト")]
        [SerializeField] private List<Selectable> selectableObjects;

        [Header("選択中ボタンに重ねるカーソル用 Image")]
        [SerializeField] private Image cursorImage;

        [Header("カーソルの位置をずらすオフセット(ピクセル単位など)")]
        [SerializeField] private Vector2 cursorOffset;

        [Header("=== Events ===")]
        [SerializeField] private UnityEvent onSelect;
        [SerializeField] private UnityEvent onConfirm;
        [SerializeField] private UnityEvent onCancel;

        private int currentIndex = 0;

        private PlayerInput playerInput;
        private InputAction selectAction;
        private InputAction confirmAction;
        private InputAction cancelAction;


        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerInput.SwitchCurrentActionMap("UI");

            // “Select” アクションを取得
            selectAction = playerInput.actions.FindAction("Select", throwIfNotFound: true);
            selectAction.started   += OnSelectStarted;
            // selectAction.performed += OnSelectPerformed;
            // selectAction.canceled  += OnSelectCanceled;

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
                selectAction.started   -= OnSelectStarted;
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
        /// 現在のリストを丸ごと差し替える（先頭要素を選択状態にする）
        /// </summary>
        public void SetSelectableObjects(List<Selectable> newList)
        {
            selectableObjects = newList;
            currentIndex = 0;
            UpdateSelection();
        }

        /// <summary>
        /// インデックス指定つきでリストを差し替える（戻ったときに前のインデックスに復帰したい場合用）
        /// </summary>
        public void SetSelectableObjects(List<Selectable> newList, int startIndex)
        {
            selectableObjects = newList;
            currentIndex = Mathf.Clamp(startIndex, 0, newList.Count - 1);
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

            Debug.Log("Confirm pressed! Selected: " + selectableObjects[currentIndex].gameObject.name);
            var button = selectableObjects[currentIndex].GetComponent<Button>();
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
            // 必要に応じて閉じる処理など
        }

        //========================
        // 選択移動関連
        //========================

        private void MoveSelectionUp()
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = selectableObjects.Count - 1;
            }
            UpdateSelection();
        }

        private void MoveSelectionDown()
        {
            currentIndex++;
            if (currentIndex >= selectableObjects.Count)
            {
                currentIndex = 0;
            }
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (cursorImage != null && selectableObjects.Count > 0)
            {
                var target = selectableObjects[currentIndex];
                cursorImage.transform.position 
                    = target.transform.position + (Vector3)cursorOffset;

                Debug.Log("Selected Button: " + target.gameObject.name);
            }
        }
    }
}