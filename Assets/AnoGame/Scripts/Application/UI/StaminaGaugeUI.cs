using UnityEngine;
using UnityEngine.UI;
using UniRx;
using AnoGame.Application.Player;

namespace AnoGame.Application.UI
{
    public class StaminaGaugeUI : MonoBehaviour
    {
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private Image gaugeImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private float _maxStaminaStayTimer;
        private bool _isFadingOut;
        private const float MaxStaminaWaitTime = 3.0f;

        private void Start()
        {
            if (playerStamina == null)
            {
                // Try to find if not assigned
                playerStamina = FindObjectOfType<PlayerStamina>();
            }

            if (playerStamina != null)
            {
                // Initial state
                UpdateGauge(playerStamina.CurrentStamina, playerStamina.MaxStamina);

                playerStamina.CurrentStaminaRx
                    .Subscribe(current => UpdateGauge(current, playerStamina.MaxStamina))
                    .AddTo(this);
            }
        }

        private void Update()
        {
            if (playerStamina == null || canvasGroup == null) return;

            float current = playerStamina.CurrentStamina;
            float max = playerStamina.MaxStamina;

            // Using approximate comparison for float equality
            if (Mathf.Approximately(current, max))
            {
                _maxStaminaStayTimer += Time.deltaTime;
                if (_maxStaminaStayTimer >= MaxStaminaWaitTime)
                {
                    FadeOut();
                }
            }
            else
            {
                _maxStaminaStayTimer = 0f;
                Show();
            }
        }

        private void UpdateGauge(float current, float max)
        {
            if (gaugeImage != null && max > 0)
            {
                gaugeImage.fillAmount = current / max;
            }
        }

        private void Show()
        {
            // If hiding or invisible, show immediately
            if (canvasGroup.alpha < 1f)
            {
                // We could tween here, but "show immediately" usually feels snappier for reaction
                // Or we can fade in quickly. Let's simpler set to 1 for responsiveness as requested "Variable only when changed" (roughly)
                // "ダッシュを使って数値の変動がある時だけ表示する" -> Show when numbers change (decreasing).
                canvasGroup.alpha = 1f;
            }
        }

        private void FadeOut()
        {
            if (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            }
        }
    }
}
