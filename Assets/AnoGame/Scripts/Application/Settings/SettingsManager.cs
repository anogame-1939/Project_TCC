using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AnoGame.Domain.Data.Models;
using AnoGame.Domain.Data.Services;
using VContainer;

namespace AnoGame.Application.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        private SettingsData _settingsData;

        public event Action<SettingsData> OnSettingsDataChanged;

        [Inject] private ISettingsDataRepository _repository;

        [Inject]
        public void Construct(ISettingsDataRepository repository)
        {
            _repository = repository;
            InitializeSettingsData().Forget();
        }

        private async UniTask InitializeSettingsData()
        {
            try
            {
                Debug.Log("InitializeSettingsData");
                _settingsData = await _repository.LoadDataAsync() ?? new SettingsData(1.0f, 1.0f, 1.0f, Language.English);
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize settings data: {ex.Message}");
                _settingsData = new SettingsData(1.0f, 1.0f, 1.0f, Language.English);
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
        }

        public SettingsData CurrentSettingsData => _settingsData;

        /// <summary>
        /// 既存の設定データを上書きします（4項目一括更新）
        /// </summary>
        public void SetSettingsData(float masterVolume, float bgmVolume, float seSoundVolume, Language language)
        {
            if (_settingsData == null)
            {
                _settingsData = new SettingsData(masterVolume, bgmVolume, seSoundVolume, language);
            }
            else
            {
                _settingsData.MasterVolume = masterVolume;
                _settingsData.BGMVolume = bgmVolume;
                _settingsData.SESoundVolume = seSoundVolume;
                _settingsData.Language = language;
            }
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        /// <summary>
        /// マスター音量のみを更新するメソッド
        /// </summary>
        public void UpdateMasterVolume(float masterVolume)
        {
            if (_settingsData == null)
            {
                // 他の値はデフォルト（1.0f, 1.0f）および言語はEnglishを仮設定
                _settingsData = new SettingsData(masterVolume, 1.0f, 1.0f, Language.English);
            }
            else
            {
                _settingsData.MasterVolume = masterVolume;
            }
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        /// <summary>
        /// BGM音量のみを更新するメソッド
        /// </summary>
        public void UpdateBgmVolume(float bgmVolume)
        {
            if (_settingsData == null)
            {
                _settingsData = new SettingsData(1.0f, bgmVolume, 1.0f, Language.English);
            }
            else
            {
                _settingsData.BGMVolume = bgmVolume;
            }
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        /// <summary>
        /// SE音量のみを更新するメソッド
        /// </summary>
        public void UpdateSeSoundVolume(float seSoundVolume)
        {
            if (_settingsData == null)
            {
                _settingsData = new SettingsData(1.0f, 1.0f, seSoundVolume, Language.English);
            }
            else
            {
                _settingsData.SESoundVolume = seSoundVolume;
            }
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        /// <summary>
        /// 1項目のstringを受け付け、Enum.TryParse を利用して Language を更新するメソッド
        /// </summary>
        public void UpdateLanguageFromString(string languageString)
        {
            if (Enum.TryParse<Language>(languageString, true, out Language languageEnum))
            {
                if (_settingsData == null)
                {
                    _settingsData = new SettingsData(1.0f, 1.0f, 1.0f, languageEnum);
                }
                else
                {
                    _settingsData.Language = languageEnum;
                }
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
            else
            {
                Debug.LogError($"UpdateLanguageFromString: 変換できない入力です -> {languageString}");
            }
        }

        public void SaveSettings()
        {
            SaveSettingsAsync().Forget();
        }

        public async UniTask SaveSettingsAsync()
        {
            if (_settingsData != null)
            {
                try
                {
                    await _repository.SaveDataAsync(_settingsData);
                    Debug.Log("Settings saved successfully");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save settings: {ex.Message}");
                }
            }
        }

        public void ResetSettings()
        {
            _settingsData = new SettingsData(1.0f, 1.0f, 1.0f, Language.English);
            OnSettingsDataChanged?.Invoke(_settingsData);
        }
    }
}
