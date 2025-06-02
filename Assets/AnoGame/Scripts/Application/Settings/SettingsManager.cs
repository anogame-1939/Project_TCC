using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AnoGame.Domain.Data.Models;
using AnoGame.Domain.Data.Services;
using VContainer;
using UnityEngine.Audio; // ★ 追加

namespace AnoGame.Application.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        private SettingsData _settingsData;

        public event Action<SettingsData> OnSettingsDataChanged;

        [Inject] private ISettingsDataRepository _repository;

        [Header("Audio Mixer")] // ★ インスペクターで設定
        [SerializeField] private AudioMixer audioMixer;

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
                var tmpsettingsData = await _repository.LoadDataAsync();
                _settingsData = tmpsettingsData ?? GetDefaultSettingsData();
                ApplyAudioMixerVolumes(); // ★ 追加：初期値反映
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize settings data: {ex.Message}");
                _settingsData = GetDefaultSettingsData();
                ApplyAudioMixerVolumes(); // ★
                OnSettingsDataChanged?.Invoke(_settingsData);
            }
        }

        public SettingsData CurrentSettingsData => _settingsData;

        public void SetSettingsData(float masterVolume, float bgmVolume, float seSoundVolume, Language language)
        {
            if (_settingsData == null)
                _settingsData = GetDefaultSettingsData();

            _settingsData.MasterVolume = masterVolume;
            _settingsData.BGMVolume = bgmVolume;
            _settingsData.SESoundVolume = seSoundVolume;
            _settingsData.Language = language;

            ApplyAudioMixerVolumes(); // ★
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        public void UpdateMasterVolume(float masterVolume)
        {
            if (_settingsData == null)
                _settingsData = GetDefaultSettingsData();

            _settingsData.MasterVolume = masterVolume;
            ApplyAudioMixerVolumes(); // ★
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        public void UpdateBgmVolume(float bgmVolume)
        {
            if (_settingsData == null)
                _settingsData = GetDefaultSettingsData();

            _settingsData.BGMVolume = bgmVolume;
            ApplyAudioMixerVolumes(); // ★
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        public void UpdateSeSoundVolume(float seSoundVolume)
        {
            if (_settingsData == null)
                _settingsData = GetDefaultSettingsData();

            _settingsData.SESoundVolume = seSoundVolume;
            ApplyAudioMixerVolumes(); // ★
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        public void UpdateLanguageFromInt(int languageValue)
        {
            if (Enum.IsDefined(typeof(Language), languageValue))
            {
                Language languageEnum = (Language)languageValue;

                if (_settingsData == null)
                    _settingsData = GetDefaultSettingsData();

                _settingsData.Language = languageEnum;
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
                    Debug.Log("Settings save now...");
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
            ApplyAudioMixerVolumes(); // ★
            OnSettingsDataChanged?.Invoke(_settingsData);
        }

        private SettingsData GetDefaultSettingsData()
        {
            return new SettingsData(1.0f, 1.0f, 1.0f, GetDefaultLanguageBasedOnSystem());
        }

        private Language GetDefaultLanguageBasedOnSystem()
        {
            switch (UnityEngine.Application.systemLanguage)
            {
                case SystemLanguage.Japanese:
                    return Language.Japanese;
                case SystemLanguage.English:
                    return Language.English;
                default:
                    return Language.English;
            }
        }

        /// <summary>
        /// AudioMixer に音量を反映させる
        /// </summary>
        private void ApplyAudioMixerVolumes()
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("AudioMixer が設定されていません。");
                return;
            }

            audioMixer.SetFloat("MasterVolume", ToDecibel(_settingsData.MasterVolume));
            audioMixer.SetFloat("BGMVolume", ToDecibel(_settingsData.BGMVolume));
            audioMixer.SetFloat("SEVolume", ToDecibel(_settingsData.SESoundVolume));
        }

        /// <summary>
        /// Linear(0〜1) を dB に変換
        /// </summary>
        private float ToDecibel(float linear)
        {
            return Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        }
    }
}
