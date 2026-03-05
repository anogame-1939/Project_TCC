using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace AnoGame.Application.UI
{
    public class InputDeviceIcon : MonoBehaviour
    {
        [Header("UI 参照")]
        [SerializeField] private Image iconImage;       // ボタンアイコンを表示する Image
        [SerializeField] private Sprite gamepadSprite;  // ゲームパッド用「A」ボタンアイコン
        [SerializeField] private Sprite keyboardSprite; // キーボード用「Space」キーアイコン

        private PlayerInput playerInput;

        private void Awake()
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
            if (iconImage == null)
                Debug.LogError("iconImage を Inspector で設定してください。");
        }

        private void OnEnable()
        {
            // コントロールスキーム切り替え時に呼ばれる
            playerInput.onControlsChanged += OnControlsChanged;
            UpdateIcon();
        }

        private void OnDisable()
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput pi)
        {
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            // 現在のコントロールスキーム名を取得
            string scheme = playerInput.currentControlScheme;

            if (scheme != null && scheme.ToLower().Contains("gamepad"))
            {
                iconImage.sprite = gamepadSprite;
            }
            else
            {
                iconImage.sprite = keyboardSprite;
            }
        }
    }
}