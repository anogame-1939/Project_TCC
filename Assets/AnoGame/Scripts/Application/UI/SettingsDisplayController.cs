using UnityEngine;
using UnityEngine.InputSystem;

namespace AnoGame.Application.UI
{
    public class SettingsDisplayController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActionAsset;
        [SerializeField] private SettingsDisplay _settingsDisplay; // SettingsDisplayがアタッチされたUIパネル

        private CanvasGroup _canvasGroup;
        private InputAction _settingsAction;

        void Start()
        {
            if (_inputActionAsset == null)
            {
                Debug.LogError("_inputActionAssetが設定されていません。");
                return;
            }
            if (_settingsDisplay == null)
            {
                Debug.LogError("_settingsDisplayが設定されていません。");
                return;
            }

            var actionMap = _inputActionAsset.FindActionMap("Player");
            actionMap.Enable();

            _settingsAction = actionMap.FindAction("Settings");
            if (_settingsAction == null)
            {
                Debug.LogError("Settingsアクションが見つかりません。");
                return;
            }
            // ToggleSettings メソッドを購読
            _settingsAction.performed += OnSettingsPerformed;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("CanvasGroupが見つかりません。");
            }
            // 初回はSettings UIを非表示にする
            HideSettings();
        }

        private void OnSettingsPerformed(InputAction.CallbackContext context)
        {
            ToggleSettings();
        }

        void OnDestroy()
        {
            // 購読解除
            if (_settingsAction != null)
            {
                _settingsAction.performed -= OnSettingsPerformed;
            }
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

        public void ShowSettings()
        {
            // SettingsDisplayが有効になるとOnEnableで表示更新が行われる想定
            _settingsDisplay.gameObject.SetActive(true);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1;

            // Settings表示中はカーソルを解放
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideSettings()
        {
            _settingsDisplay.gameObject.SetActive(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0;

            // Settings非表示時はカーソルをロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            return;
            /*
            if (hasFocus)
            {
                // if (_isSettingsOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            */
        }
    }
}
