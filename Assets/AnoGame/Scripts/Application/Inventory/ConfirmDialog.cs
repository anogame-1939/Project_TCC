// ConfirmDialog（UGUI）
using System;
using AnoGame.Application.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public sealed class ConfirmDialog : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    InputAction _confirm, _cancel;
    Action _onYes, _onNo;
    
    [Inject] private IInputActionProvider _inputProvider;

    public void Show(string title, Action onYes, Action onNo)
    {
        titleText.text = title;
        _onYes = onYes; _onNo = onNo;
        gameObject.SetActive(true);

        // UIマップから直接購読（自前で解除まで行う）
        var ui = _inputProvider.GetUIActionMap();
        _confirm = ui.FindAction("Confirm", true);
        _cancel = ui.FindAction("Cancel", true);
        _confirm.performed += OnSubmit;
        _cancel.performed += OnCancel;

        // ボタンでも押せるように
        yesButton.onClick.AddListener(() => OnSubmit(default));
        noButton.onClick.AddListener(() => OnCancel(default));

        EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
    }

    public void Hide()
    {
        _confirm.performed -= OnSubmit;
        _cancel .performed -= OnCancel;
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    void OnSubmit(InputAction.CallbackContext _){ _onYes?.Invoke(); Hide(); }
    void OnCancel(InputAction.CallbackContext _){ _onNo?.Invoke();  Hide(); }
}
