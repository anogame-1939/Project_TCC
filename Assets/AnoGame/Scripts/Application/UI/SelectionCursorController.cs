using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Events;

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
    // Selectアクション時のイベント
    [SerializeField] private UnityEvent onSelect;
    // Confirmアクション時のイベント
    [SerializeField] private UnityEvent onConfirm;
    // Cancelアクション時のイベント
    [SerializeField] private UnityEvent onCancel;

    private int currentIndex = 0;

    private PlayerInput playerInput;

    // “Select” “Confirm” “Cancel” アクションを参照
    private InputAction selectAction;
    private InputAction confirmAction;
    private InputAction cancelAction;

    private bool isKeyHeld = false;

    [SerializeField] private float initialDelay = 0.5f;  // 最初のリピート開始までの待ち時間
    [SerializeField] private float repeatInterval = 0.1f; // リピートの間隔

    private float nextMoveTime = 0f;
    private Vector2 currentInput = Vector2.zero;
    private bool isHoldingDirection = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        // UI アクションマップに切り替え
        playerInput.SwitchCurrentActionMap("UI");

        // “Select” アクションを取得
        selectAction = playerInput.actions.FindAction("Select", throwIfNotFound: true);
        selectAction.started   += OnSelectStarted;
         selectAction.performed += OnSelectPerformed;
        selectAction.canceled  += OnSelectCanceled;

        // “Confirm” アクションを取得
        confirmAction = playerInput.actions.FindAction("Confirm", throwIfNotFound: true);
        confirmAction.performed += OnConfirmPerformed;

        // “Cancel” アクションを取得
        cancelAction = playerInput.actions.FindAction("Cancel", throwIfNotFound: true);
        cancelAction.performed += OnCancelPerformed;
    }

    private void OnDestroy()
    {
        // イベント購読解除
        if (selectAction != null)
        {
            selectAction.started   -= OnSelectStarted;
            // selectAction.performed -= OnSelectPerformed;
            selectAction.canceled  -= OnSelectCanceled;
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

    //========================
    // Selectアクションの処理
    //========================

    private void OnSelectStarted(InputAction.CallbackContext context)
    {
        // ★ イベント呼び出し (Selectアクション時)
        onSelect?.Invoke();

        Vector2 inputValue = context.ReadValue<Vector2>();
        Debug.Log("Select started! Input: " + inputValue);
        if (inputValue.y > 0)
        {
            MoveSelectionUp();
        }
        else if (inputValue.y < 0)
        {
            MoveSelectionDown();
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext context)
    {
        if (!isKeyHeld) return;

        // ★ イベント呼び出し (Selectアクション時)
        onSelect?.Invoke();

        Vector2 inputValue = context.ReadValue<Vector2>();
        if (inputValue.y > 0.5f)
        {
            MoveSelectionUp();
        }
        else if (inputValue.y < -0.5f)
        {
            MoveSelectionDown();
        }
        // 左右入力を使うなら inputValue.x も判定
    }

    private void OnSelectCanceled(InputAction.CallbackContext context)
    {
        isKeyHeld = false;
    }

    //========================
    // Confirmアクションの処理
    //========================

    private void OnConfirmPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Confirm pressed! Selected: " + selectableObjects[currentIndex].gameObject.name);

        // ★ イベント呼び出し (Confirmアクション時)
        onConfirm?.Invoke();

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
        Debug.Log("Cancel pressed!");

        // ★ イベント呼び出し (Cancelアクション時)
        onCancel?.Invoke();

        // 必要に応じて、UIを閉じる・前の画面に戻るなどの処理を行う
        // gameObject.SetActive(false);
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
        if (cursorImage != null && selectableObjects[currentIndex] != null)
        {
            cursorImage.transform.position 
                = selectableObjects[currentIndex].transform.position + (Vector3)cursorOffset;
        }

        Debug.Log("Selected Button: " + selectableObjects[currentIndex].gameObject.name);
    }
}
