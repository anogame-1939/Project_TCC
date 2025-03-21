using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using AnoGame.Domain.Data.Models;
using AnoGame.Domain.Data.Services;
using VContainer;

namespace AnoGame.Application.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        private SettingsData _settingsData;

        [Inject] private ISettingsDataRepository _repository;

        [Inject]
        public void Construct(ISettingsDataRepository repository)
        {
            _repository = repository;
            InitializeSettingsData().Forget();
        }

        /// <summary>
        /// 非同期で設定データをロードし、存在しない場合は初期値を設定する
        /// </summary>
        private async UniTask InitializeSettingsData()
        {
            try
            {
                Debug.Log("InitializeSettingsData");
                _settingsData = await _repository.LoadDataAsync() ?? new SettingsData(1.0f, 1.0f, 1.0f, Language.English);
                Debug.Log($"Settings data loaded: {_settingsData}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize settings data: {ex.Message}");
                _settingsData = new SettingsData(1.0f, 1.0f, 1.0f, Language.English);
            }
        }

        /// <summary>
        /// 現在の設定データを取得します
        /// </summary>
        public SettingsData CurrentSettingsData => _settingsData;

        /// <summary>
        /// 設定データに各種設定値を反映します
        /// </summary>
        /// <param name="masterVolume">マスター音量</param>
        /// <param name="bgmVolume">BGM音量</param>
        /// <param name="seSoundVolume">SE音量</param>
        /// <param name="language">言語設定</param>
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
        }

        /// <summary>
        /// 現在の設定データを非同期で保存します
        /// </summary>
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

        /// <summary>
        /// 設定データを初期状態（マスター、BGM、SEともに1.0f、言語はEnglish）にリセットします
        /// </summary>
        public void ResetSettings()
        {
            _settingsData = new SettingsData(1.0f, 1.0f, 1.0f, Language.English);
        }
    }
}
