using System;
using Newtonsoft.Json;

namespace AnoGame.Domain.Data.Models
{
    public enum Language
    {
        Japanese,
        English,
        // 他の言語を追加する場合はここに記述
    }

    [Serializable]
    public class SettingsData
    {
        [JsonProperty]
        public float MasterVolume { get; set; }
        [JsonProperty]
        public float BGMVolume { get; set; }
        [JsonProperty]
        public float SESoundVolume { get; set; }
        [JsonProperty]
        public Language Language { get; set; }

        [JsonConstructor]
        public SettingsData() { }

        public SettingsData(float masterVolume, float bgmVolume, float seSoundVolume, Language language)
        {
            MasterVolume = masterVolume;
            BGMVolume = bgmVolume;
            SESoundVolume = seSoundVolume;
            Language = language;
        }
    }
}
