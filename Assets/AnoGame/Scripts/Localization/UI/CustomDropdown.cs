using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Localizer.UI
{
    [System.Serializable]
    public class LanguageOption
    {
        public string displayName;   // Dropdownで表示する名前
        public TMP_FontAsset font;   // 切り替え先フォント
        // public Locale locale;    // Localizationパッケージを併用するなら、Locale情報も持たせる
    }

    public class CustomDropdown : MonoBehaviour
    {
        public TMP_Dropdown dropdown;
        public TextMeshProUGUI sampleText;
        public List<LanguageOption> languageOptions;

        void Start()
        {
            // Dropdownに言語名をセット
            List<TMP_Dropdown.OptionData> optionList = new List<TMP_Dropdown.OptionData>();
            foreach (var langOpt in languageOptions)
            {
                optionList.Add(new TMP_Dropdown.OptionData(langOpt.displayName));
            }
            dropdown.AddOptions(optionList);
            dropdown.RefreshShownValue();
        }
    }
}