using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Localizer
{
    /// <summary>
    /// シーン上のTextMeshProUGUIに翻訳テキストを適用するクラス
    /// </summary>
    public class JapaneseObjectSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject japaneseObject;
        [SerializeField] private GameObject otherLangObject;

        private void Awake()
        {
            japaneseObject.SetActive(false);
            otherLangObject.SetActive(false);

        }

        
        public void ApplyLocalize()
        {
            // 現在選択中のロケールを取得
            Locale current = LocalizationSettings.SelectedLocale;
            // コード(例: "ja", "en", "fr")を取り出して日本語かどうか判定
            bool isJapanese = current.Identifier.Code.Equals("ja");
            if (isJapanese)
            {
                japaneseObject.SetActive(true);
            }
            else
            {
                otherLangObject.SetActive(true);
            }
        }

    }
}
