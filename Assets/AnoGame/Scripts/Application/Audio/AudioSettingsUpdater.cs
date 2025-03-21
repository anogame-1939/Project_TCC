using UnityEngine;
using AnoGame.Application.Settings;
using AnoGame.Domain.Data.Models;
using VContainer;

namespace AnoGame.Application.Audio
{
    public class AudioSettingsUpdater : MonoBehaviour
    {
        [Inject] private SettingsManager settingsManager;

        private void OnEnable()
        {
            if (settingsManager != null)
            {
                settingsManager.OnSettingsDataChanged += ApplyAudioSettings;
                // 初回更新
                ApplyAudioSettings(settingsManager.CurrentSettingsData);
            }
        }

        private void OnDisable()
        {
            if (settingsManager != null)
            {
                settingsManager.OnSettingsDataChanged -= ApplyAudioSettings;
            }
        }

        /// <summary>
        /// 設定データの変更時に呼ばれ、BGMタグとSEタグのオブジェクトの音量を更新します
        /// </summary>
        /// <param name="data">現在の設定データ</param>
        private void ApplyAudioSettings(SettingsData data)
        {
            if (data == null) return;

            // 設定から有効な音量を算出
            float effectiveBgmVolume = data.MasterVolume * data.BGMVolume;
            float effectiveSeVolume = data.MasterVolume * data.SESoundVolume;

            // タグ "BGM" のオブジェクトをすべて取得して音量を設定
            GameObject[] bgmObjects = GameObject.FindGameObjectsWithTag("BGM");
            foreach (var obj in bgmObjects)
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume = effectiveBgmVolume;
                }
            }

            // タグ "SE" のオブジェクトをすべて取得して音量を設定
            GameObject[] seObjects = GameObject.FindGameObjectsWithTag("SE");
            foreach (var obj in seObjects)
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume = effectiveSeVolume;
                }
            }
        }
    }
}
