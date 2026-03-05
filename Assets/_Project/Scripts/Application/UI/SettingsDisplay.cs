using UnityEngine;
using UnityEngine.UI;
using AnoGame.Application.Settings;
using AnoGame.Domain.Data.Models;
using VContainer;
using TMPro;

namespace AnoGame.Application.UI
{
    public class SettingsDisplay : MonoBehaviour
    {
        // 音量用のスクロールバー（0〜1の範囲を前提）
        [SerializeField] private Scrollbar masterVolumeScrollbar;
        [SerializeField] private Scrollbar bgmVolumeScrollbar;
        [SerializeField] private Scrollbar seVolumeScrollbar;

        // 言語設定用のドロップダウン（Language列挙型の順番に合わせたオプションを用意すること）
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Inject] private SettingsManager settingsManager;

        private void OnEnable()
        {
            if (settingsManager != null)
            {
                settingsManager.OnSettingsDataChanged += UpdateDisplay;
                // 初回表示のため、現在の設定データで更新
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

        /// <summary>
        /// 設定データを受け取り、各UI要素に反映します
        /// </summary>
        /// <param name="data">現在の設定データ</param>
        private void UpdateDisplay(SettingsData data)
        {
            if (data == null) return;

            // 各音量のスクロールバーの値を更新（0〜1の範囲と仮定）
            if (masterVolumeScrollbar != null)
                masterVolumeScrollbar.value = data.MasterVolume;
            if (bgmVolumeScrollbar != null)
                bgmVolumeScrollbar.value = data.BGMVolume;
            if (seVolumeScrollbar != null)
                seVolumeScrollbar.value = data.SESoundVolume;

            // ドロップダウンは、Language列挙型の int 値を利用して選択状態を更新
            if (languageDropdown != null)
                languageDropdown.value = (int)data.Language;
        }
    }
}
