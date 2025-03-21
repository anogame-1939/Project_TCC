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
        /// 既存の設定データを上書きします
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
        /// マスター音量から言語までの各値を一括で更新します
        /// </summary>
        public void UpdateSettings(float masterVolume, float bgmVolume, float seSoundVolume, Language language)
        {
            // _settingsData が null なら新規作成、既にある場合は各値を更新
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
            // 設定データの変更を購読している側に通知
            OnSettingsDataChanged?.Invoke(_settingsData);
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
