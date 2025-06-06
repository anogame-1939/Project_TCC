using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using AnoGame.Application.Input;
using UnityEngine.Events;

namespace AnoGame.Application.UI
{
    public class SettingsDisplayController : MonoBehaviour
    {
        [SerializeField] private SettingsDisplay _settingsDisplay; // 設定パネル
        [SerializeField] private UnityEvent showEvents;
        [SerializeField] private UnityEvent hideEvents;
        [SerializeField] private UnityEvent cancelEvents;

        private CanvasGroup _canvasGroup;
        private InputAction _settingsOpenAction;    // Player マップ上の Settings
        private InputAction _settingsCloseAction1;  // UI マップ上の Cancel
        private InputAction _settingsCloseAction2;  // UI マップ上の Settings

        //──────────────────────────────────────────
        // ① IInputActionProvider を Inject で受け取る
        //──────────────────────────────────────────
        [Inject] private IInputActionProvider _inputProvider;

        void Start()
        {
            if (_settingsDisplay == null)
            {
                Debug.LogError("[SettingsDisplayController] _settingsDisplay が設定されていません。");
                return;
            }

            //──────────────────────────────────────────
            // ② 最初は「Player」アクションマップを有効にしておく
            //──────────────────────────────────────────
            _inputProvider.SwitchToPlayer();


            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("[SettingsDisplayController] CanvasGroup がアタッチされていません。");
            }
            HideSettings();

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var playerMap = _inputProvider.GetPlayerActionMap();
            _settingsOpenAction = playerMap.FindAction("Settings", throwIfNotFound: true);
            _settingsOpenAction.performed += OnSettingsOpenPerformed;

            var uiMap = _inputProvider.GetUIActionMap();
            _settingsCloseAction1 = uiMap.FindAction("Cancel", throwIfNotFound: true);
            _settingsCloseAction1.performed += OnSettingsClosePerformed;
            _settingsCloseAction2 = uiMap.FindAction("Settings", throwIfNotFound: false);
            _settingsCloseAction2.performed += OnSettingsClosePerformed;
        }

        private void Unsubscribe()
        {
            var playerMap = _inputProvider.GetPlayerActionMap();
            _settingsOpenAction = playerMap.FindAction("Settings", throwIfNotFound: true);
            _settingsOpenAction.performed -= OnSettingsOpenPerformed;

            var uiMap = _inputProvider.GetUIActionMap();
            _settingsCloseAction1 = uiMap.FindAction("Cancel", throwIfNotFound: true);
            _settingsCloseAction1.performed -= OnSettingsClosePerformed;
            _settingsCloseAction2 = uiMap.FindAction("Settings", throwIfNotFound: false);
            _settingsCloseAction2.performed -= OnSettingsClosePerformed;
        }

        //──────────────────────────────────────────
        // Player マップで Settings を押したとき
        //──────────────────────────────────────────
        private void OnSettingsOpenPerformed(InputAction.CallbackContext context)
        {
            ToggleSettings();
        }

        void ToggleSettings()
        {
            var currentState = GameStateManager.Instance.CurrentState;

            if (currentState == GameState.Gameplay)
            {
                // ────────────────
                // Gameplay → Settings
                // ────────────────
                GameStateManager.Instance.SetState(GameState.Settings);
                ShowSettings();
            }
            else if (currentState == GameState.Settings)
            {
                // ────────────────
                // Settings → Gameplay
                // ────────────────
                GameStateManager.Instance.SetState(GameState.Gameplay);
                HideSettings();
            }
        }

        public void Close()
        {
            // ボタンなどから直接閉じたいときも一応こちらを使う
            GameStateManager.Instance.SetState(GameState.Gameplay);
            HideSettings();
        }

        //──────────────────────────────────────────
        // 設定画面を開く
        //  → UIマップを有効化して、UI/Cancel と UI/Settings の両方を購読
        //──────────────────────────────────────────
        public void ShowSettings()
        {
            // (1) UIマップへ切り替え
            _inputProvider.SwitchToUI();

            // (3) UI 表示＆カーソル開放
            // _settingsDisplay.gameObject.SetActive(true);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1;

            showEvents?.Invoke();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        //──────────────────────────────────────────
        // 設定画面を閉じる
        //  → UI/Cancel と UI/Settings の購読解除 ＆ Playerマップへ戻す
        //──────────────────────────────────────────
        public void HideSettings()
        {
            // (1) UI表示をオフにして、カーソルをロック
            // _settingsDisplay.gameObject.SetActive(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;



            hideEvents?.Invoke();

            // (3) Playerマップへ切り替え
            _inputProvider.SwitchToPlayer();
        }

        //──────────────────────────────────────────
        // UI/Cancel または UI/Settings が呼ばれたとき（EscやBボタンで閉じる）
        //──────────────────────────────────────────
        private void OnSettingsClosePerformed(InputAction.CallbackContext context)
        {
            // Settings → Gameplay へ切り替え
            GameStateManager.Instance.SetState(GameState.Gameplay);
            // HideSettings();
            cancelEvents?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // フォーカス制御は不要ならそのまま
            return;
        }
    }
}
