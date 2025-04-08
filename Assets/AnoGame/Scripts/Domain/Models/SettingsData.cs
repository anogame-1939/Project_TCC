using System;
using Newtonsoft.Json;

namespace AnoGame.Domain.Data.Models
{
    public enum Language
    {
        ChineseCN,    // Chinese (CN)
        ChineseHK,    // Chinese (HK)
        English,      // English
        Suomi,        // Suomi (Finnish)
        French,       // Français
        Hindi,        // Hindi
        Indonesian,   // Indonesian
        Italian,      // Italian
        Japanese,     // Japanese
        Korean,       // Korean
        Portuguese,   // Portuguese
        Russian,      // Russian
        Spanish       // Spanish
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
