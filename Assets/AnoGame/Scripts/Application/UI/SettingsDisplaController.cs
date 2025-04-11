using UnityEngine;
using UnityEngine.InputSystem;

namespace AnoGame.Application.UI
{
    public class SettingsDisplaController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActionAsset;
        [SerializeField] private SettingsDisplay _settingsDisplay; // SettingsDisplayがアタッチされたUIパネル

        private CanvasGroup _canvasGroup;
        private bool _isSettingsOpen = false;

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

            var settingsAction = actionMap.FindAction("Settings");
            if (settingsAction == null)
            {
                Debug.LogError("Settingsアクションが見つかりません。");
                return;
            }
            settingsAction.performed += ctx => ToggleSettings();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("CanvasGroupが見つかりません。");
            }
            // 初回はSettings UIを非表示にする
            HideSettings();
        }

        void ToggleSettings()
        {
            if (_isSettingsOpen)
            {
                Debug.Log("Settingsを閉じます");
                HideSettings();
            }
            else
            {
                Debug.Log("Settingsを開きます");
                ShowSettings();
            }
        }

        public void ShowSettings()
        {
            _isSettingsOpen = true;
            // SettingsDisplayが有効になるとOnEnableで表示更新が行われる想定
            _settingsDisplay.gameObject.SetActive(true);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1;

            // Settings表示中はカーソルを開放
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
        }

        public void HideSettings()
        {
            _isSettingsOpen = false;
            _settingsDisplay.gameObject.SetActive(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0;

            // Settings非表示時はカーソルをロック
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            /*
            if (hasFocus)
            {
                if (_isSettingsOpen)
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
