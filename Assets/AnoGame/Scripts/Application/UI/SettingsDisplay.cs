using UnityEngine;
using TMPro;
using AnoGame.Application.Settings;
using AnoGame.Domain.Data.Models;
using VContainer;

namespace AnoGame.Application.UI
{
    public class SettingsDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text settingsText;

        [Inject] private SettingsManager settingsManager;

        private void OnEnable()
        {
            if (settingsManager != null)
            {
                settingsManager.OnSettingsDataChanged += UpdateDisplay;
                // 初回表示のため現在の設定データで更新
                UpdateDisplay(settingsManager.CurrentSettingsData);
            }
        }

        private void OnDisable()
        {
            if (settingsManager != null)
            {
                settingsManager.OnSettingsDataChanged -= UpdateDisplay;
            }
        }

        private void UpdateDisplay(SettingsData data)
        {
            if (settingsText != null && data != null)
            {
                // 例：各設定値をテキストで表示
                settingsText.text = $"Master: {data.MasterVolume}\nBGM: {data.BGMVolume}\nSE: {data.SESoundVolume}\nLanguage: {data.Language}";
            }
        }
    }
}
