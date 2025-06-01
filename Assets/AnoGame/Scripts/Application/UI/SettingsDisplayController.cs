using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using AnoGame.Application.Input; // IInputActionProvider の名前空間

namespace AnoGame.Application.UI
{
    public class SettingsDisplayController : MonoBehaviour
    {
        [SerializeField] private SettingsDisplay _settingsDisplay; // 設定パネル

        private CanvasGroup _canvasGroup;
        private InputAction _settingsAction;

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
            var playerMap = _inputProvider.GetPlayerActionMap();

            //──────────────────────────────────────────
            // ③ Playerマップから "Settings" アクションを探して購読
            //──────────────────────────────────────────
            _settingsAction = playerMap.FindAction("Settings", throwIfNotFound: true);
            _settingsAction.performed += OnSettingsPerformed;

            //──────────────────────────────────────────
            // ④ CanvasGroup をキャッシュして初期非表示にする
            //──────────────────────────────────────────
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("[SettingsDisplayController] CanvasGroup がアタッチされていません。");
            }
            HideSettings();
        }

        private void OnDestroy()
        {
            if (_settingsAction != null)
            {
                _settingsAction.performed -= OnSettingsPerformed;
            }
        }

        private void OnSettingsPerformed(InputAction.CallbackContext context)
        {
            ToggleSettings();
        }

        void ToggleSettings()
        {
            var currentState = GameStateManager.Instance.CurrentState;

            if (currentState == GameState.Gameplay)
            {
                GameStateManager.Instance.SetState(GameState.Settings);
                ShowSettings();
            }
            else if (currentState == GameState.Settings)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
                HideSettings();
            }
        }

        public void Close()
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
            HideSettings();
        }

        public void ShowSettings()
        {
            //──────────────────────────────────────────
            // 設定画面表示と同時に「UI」アクションマップへ切り替え
            //──────────────────────────────────────────
            _inputProvider.SwitchToUI();

            _settingsDisplay.gameObject.SetActive(true);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        public void HideSettings()
        {
            _settingsDisplay.gameObject.SetActive(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            //──────────────────────────────────────────
            // 設定画面非表示と同時に「Player」アクションマップへ切り替え
            //──────────────────────────────────────────
            _inputProvider.SwitchToPlayer();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // フォーカス制御は不要ならそのまま
            return;
        }
    }
}
