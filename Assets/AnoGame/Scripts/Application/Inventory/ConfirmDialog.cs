using System;
using System.Collections;
using AnoGame.Application.Input;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public sealed class ConfirmDialog : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Default callbacks (always run)")]
    [SerializeField] UnityEvent onYesDefault;
    [SerializeField] UnityEvent onNoDefault;

    // 任意：表示/非表示のフックも欲しければ
    [SerializeField] UnityEvent onShown;
    [SerializeField] UnityEvent onHidden;

    InputAction _confirm, _cancel;
    Action _onYes, _onNo;
    bool _canAcceptInput = false; // ← クールタイム制御フラグ

    [Inject] private IInputActionProvider _inputProvider;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.ignoreParentGroups = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        gameObject.SetActive(false);

        // クリックは統一メソッドへ（重複防止に先に一度クリア）
        yesButton.onClick.RemoveListener(InvokeYes);
        noButton.onClick.RemoveListener(InvokeNo);
        yesButton.onClick.AddListener(InvokeYes);
        noButton.onClick.AddListener(InvokeNo);
    }

    public void Show(string title, Action onYes, Action onNo)
    {
        titleText.text = title;
        _onYes = onYes; _onNo = onNo;
        gameObject.SetActive(true);

        var ui = _inputProvider.GetUIActionMap();
        _confirm = ui.FindAction("Confirm", true);
        _cancel = ui.FindAction("Cancel", true);

        _confirm.performed += OnSubmit; // キー入力も同じ経路へ
        _cancel.performed += OnCancel;

        EventSystem.current.SetSelectedGameObject(yesButton.gameObject);

        onShown?.Invoke();                          // ← 表示時既定処理
        StartCoroutine(InputCooldownCoroutine(0.5f));
    }

    public void Hide()
    {
        _confirm.performed -= OnSubmit;
        _cancel.performed -= OnCancel;
        // （ボタンの onClick は Awake で一度だけ登録しているのでここでは触らない）
        gameObject.SetActive(false);
        onHidden?.Invoke();                         // ← 非表示時既定処理
    }

    void OnSubmit(InputAction.CallbackContext _) => InvokeYes();
    void OnCancel(InputAction.CallbackContext _) => InvokeNo();

    void InvokeYes()
    {
        if (!_canAcceptInput) return;
        try
        {
            onYesDefault?.Invoke();   // ① 既定（Inspector）
            _onYes?.Invoke();         // ② 呼び出し側（Action）
        }
        finally { Hide(); }           // ③ クローズは必ず
    }

    void InvokeNo()
    {
        if (!_canAcceptInput) return;
        try
        {
            onNoDefault?.Invoke();    // ① 既定
            _onNo?.Invoke();          // ② 任意
        }
        finally { Hide(); }
    }

    IEnumerator InputCooldownCoroutine(float delay)
    {
        _canAcceptInput = false;
        yield return new WaitForSeconds(delay);
        _canAcceptInput = true;
    }
}
