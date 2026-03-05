using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using AnoGame.Application.Input;

/// <summary>
/// モーダルな確認ダイアログ。
/// - Cancel入力は UiInputRouter のスタックで最前面が“消費”
/// - Confirm入力は UI ActionMap の "Confirm" を購読（ボタンと同等）
/// - 表示/非表示時に CanvasGroup を制御し、EventSystem の選択も管理
/// </summary>
namespace AnoGame.Application.Inventory
{
    public sealed class ConfirmDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Integration")]
        [SerializeField] private UiInputRouter router; // シーン上の共通Routerをアサイン

        [Header("Behavior")]
        [SerializeField, Tooltip("表示直後、誤入力を防ぐための受付ディレイ(秒)")]
        private float inputCooldownSeconds = 0.25f;

        [Header("Default callbacks (always run first)")]
        [SerializeField] private UnityEvent onYesDefault;
        [SerializeField] private UnityEvent onNoDefault;

        [Header("Optional hooks")]
        [SerializeField] private UnityEvent onShown;
        [SerializeField] private UnityEvent onHidden;

        // DI
        [Inject] private IInputActionProvider _inputProvider;

        // 状態
        public bool IsOpen { get; private set; }
        private bool _canAcceptInput;
        private Action _onYes, _onNo;
        private IDisposable _cancelToken;     // Routerに積んだハンドラの解除トークン
        private InputAction _confirmAction;   // UI ActionMap の "Confirm"

        void Awake()
        {
            // 参照チェック
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (yesButton == null || noButton == null)
            {
                Debug.LogError("[ConfirmDialog] yesButton / noButton の参照が不足しています。");
            }
            if (router == null)
            {
                Debug.LogError("[ConfirmDialog] UiInputRouter の参照が未設定です。");
            }

            // ボタンのクリックをメソッドに集約
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(InvokeYes);
            }
            if (noButton != null)
            {
                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(InvokeNo);
            }

            // 初期は非表示
            SetVisible(false, instant: true);
            IsOpen = false;
            _canAcceptInput = false;
        }

        /// <summary>
        /// ダイアログ表示。CancelはRouterで最優先No扱い、ConfirmはUI.ActionMapの"Confirm"を購読。
        /// </summary>
        public void Show(string title, Action onYes, Action onNo)
        {
            if (titleText != null) titleText.text = title ?? string.Empty;
            _onYes = onYes;
            _onNo = onNo;

            // 可視化
            SetVisible(true);
            IsOpen = true;
            onShown?.Invoke();

            // CancelはRouterのスタック最上段として“消費”する
            if (router != null)
            {
                _cancelToken = router.PushCancelHandler(() =>
                {
                    if (!IsOpen) return false;
                    InvokeNo();         // Cancel = No
                    return true;        // ここで消費（下層に落とさない）
                });
            }

            // ConfirmはUI ActionMapから取得して購読（ボタンと同等）
            try
            {
                var uiMap = _inputProvider?.GetUIActionMap();
                _confirmAction = uiMap?.FindAction("Confirm", throwIfNotFound: true);
                if (_confirmAction != null)
                {
                    // _confirmAction.performed += OnSubmit;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfirmDialog] Confirmアクション購読に失敗: {e.Message}");
            }

            // フォーカスをYesに（パッド/キーボード操作の起点にする）
            if (yesButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(yesButton.gameObject);
            }

            // 誤入力ガード
            StopAllCoroutines();
            StartCoroutine(InputCooldownCoroutine(inputCooldownSeconds));
        }

        /// <summary>非表示（購読解除・RouterからPop）</summary>
        public void Hide()
        {
            // Confirm購読解除（nullガード）
            if (_confirmAction != null)
            {
                _confirmAction.performed -= OnSubmit;
                _confirmAction = null;
            }

            // Routerスタックから自分のハンドラを外す
            _cancelToken?.Dispose();
            _cancelToken = null;

            // 可視化OFF
            SetVisible(false);
            IsOpen = false;
            onHidden?.Invoke();

            // 入力ガード停止
            _canAcceptInput = false;
            StopAllCoroutines();
        }

        // ---- UI/入力ハンドラ -------------------------------------------------

        private void OnSubmit(InputAction.CallbackContext _)
        {
            InvokeYes();
        }

        public void InvokeYes()
        {
            if (!IsOpen || !_canAcceptInput) return;

            try
            {
                onYesDefault?.Invoke();   // 常に先に呼ぶ
                _onYes?.Invoke();         // 呼び出し側コールバック
            }
            finally
            {
                Hide();
            }
        }

        public void InvokeNo()
        {
            if (!IsOpen || !_canAcceptInput) return;

            try
            {
                onNoDefault?.Invoke();    // 常に先に呼ぶ
                _onNo?.Invoke();          // 呼び出し側コールバック
            }
            finally
            {
                Hide();
            }
        }

        // ---- 内部ユーティリティ ----------------------------------------------

        private void SetVisible(bool visible, bool instant = false)
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible; // モーダルなので可視時はレイキャストON
        }

        private IEnumerator InputCooldownCoroutine(float delay)
        {
            _canAcceptInput = false;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            _canAcceptInput = true;
        }

        // 安全のため、オブジェクト破棄時にも購読解除
        private void OnDestroy()
        {
            if (_confirmAction != null)
            {
                _confirmAction.performed -= OnSubmit;
                _confirmAction = null;
            }
            _cancelToken?.Dispose();
            _cancelToken = null;
        }
    }
}