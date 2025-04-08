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
                Debug.Log($"InitializeSettingsData");
                var tmpsettingsData = await _repository.LoadDataAsync();
                if (tmpsettingsData != null)
                {
                    Debug.Log($"InitializeSettingsData:tmpsettingsData:{tmpsettingsData}");
                    Debug.Log($"InitializeSettingsData:tmpsettingsData.Language:{tmpsettingsData.Language}");
                }
                else
                {
                    Debug.Log($"InitializeSettingsData:tmpsettingsData is null");
                }
                _settingsData = tmpsettingsData ?? new SettingsData(1.0f, 1.0f, 1.0f, GetDefaultLanguageBasedOnSystem());
                Debug.Log($"InitializeSettingsData-_settingsData.Language:{_settingsData.Language}");
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize settings data: {ex.Message}");
                _settingsData = new SettingsData(1.0f, 1.0f, 1.0f, GetDefaultLanguageBasedOnSystem());
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
                _settingsData = GetDefaultSettingsData();
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
                _settingsData = GetDefaultSettingsData();
                _settingsData.MasterVolume = masterVolume;
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
                _settingsData = GetDefaultSettingsData();
                _settingsData.BGMVolume = bgmVolume;
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
                _settingsData = GetDefaultSettingsData();
                _settingsData.SESoundVolume = seSoundVolume;
            }
            else
            {
                _settingsData.SESoundVolume = seSoundVolume;
            }
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        /// <summary>
        /// int型を受け付け、受け取った値を Language enum に変換して更新するメソッド
        /// </summary>
        public void UpdateLanguageFromInt(int languageValue)
        {
            if (Enum.IsDefined(typeof(Language), languageValue))
            {
                Language languageEnum = (Language)languageValue;
                if (_settingsData == null)
                {
                    _settingsData = GetDefaultSettingsData();
                    _settingsData.Language = languageEnum;
                }
                else
                {
                    _settingsData.Language = languageEnum;
                }
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
            else
            {
                Debug.LogError($"UpdateLanguageFromInt: 入力された値 {languageValue} は有効な Language ではありません。");
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
            _settingsData = GetDefaultSettingsData();
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        private SettingsData GetDefaultSettingsData()
        {
            return new SettingsData(1.0f, 1.0f, 1.0f, GetDefaultLanguageBasedOnSystem());
        }

        private Language GetDefaultLanguageBasedOnSystem()
        {
            switch(UnityEngine.Application.systemLanguage)
            {
                case SystemLanguage.Japanese:
                    return Language.Japanese;
                case SystemLanguage.English:
                    return Language.English;
                // 必要に応じて他の言語も追加
                default:
                    return Language.English; // デフォルトは English に設定
            }
        }
    }
}
