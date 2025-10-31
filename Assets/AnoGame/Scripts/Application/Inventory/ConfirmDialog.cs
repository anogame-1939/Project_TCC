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

    [Header("Optional hooks")]
    [SerializeField] UnityEvent onShown;
    [SerializeField] UnityEvent onHidden;

    InputAction _confirm, _cancel;
    Action _onYes, _onNo;
    bool _canAcceptInput = false;

    [Inject] private IInputActionProvider _inputProvider;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false, instant: true); // 最初は非表示

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(InvokeYes);
        noButton.onClick.AddListener(InvokeNo);
    }

    public void Show(string title, Action onYes, Action onNo)
    {
        titleText.text = title;
        _onYes = onYes; 
        _onNo = onNo;

        SetVisible(true);
        onShown?.Invoke();

        var ui = _inputProvider.GetUIActionMap();
        _confirm = ui.FindAction("Confirm", true);
        _cancel = ui.FindAction("Cancel", true);

        _confirm.performed += OnSubmit;
        _cancel.performed += OnCancel;

        EventSystem.current?.SetSelectedGameObject(yesButton.gameObject);

        StartCoroutine(InputCooldownCoroutine(0.5f));
    }

    public void Hide()
    {
        _confirm.performed -= OnSubmit;
        _cancel.performed -= OnCancel;

        SetVisible(false);
        onHidden?.Invoke();
    }

    void SetVisible(bool visible, bool instant = false)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    void OnSubmit(InputAction.CallbackContext _) => InvokeYes();
    void OnCancel(InputAction.CallbackContext _) => InvokeNo();

    void InvokeYes()
    {
        if (!_canAcceptInput) return;
        try
        {
            onYesDefault?.Invoke();
            _onYes?.Invoke();
        }
        finally { Hide(); }
    }

    void InvokeNo()
    {
        if (!_canAcceptInput) return;
        try
        {
            onNoDefault?.Invoke();
            _onNo?.Invoke();
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
