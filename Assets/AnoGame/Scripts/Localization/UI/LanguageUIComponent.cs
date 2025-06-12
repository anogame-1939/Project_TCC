using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Localizer.UI
{
    public class LanguageUIComponent : MonoBehaviour
    {
        [Header("表示先の TextMeshPro")]
        [SerializeField] private TMP_Text buttonTxt;

        [Header("ローカライズ設定")]
        [SerializeField] private LocalizedString localizedLanguageName;               // 言語名用 String TableEntry
        [SerializeField] private LocalizedAsset<TMP_FontAsset> localizedFontAsset;   // フォント用 Asset TableEntry
        [SerializeField] private LocalizedAsset<Material> localizedMaterialAsset;    // （必要なら）マテリアル用 Asset TableEntry

        private void Awake()
        {
            // イベント購読
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            localizedLanguageName.StringChanged += OnLanguageNameChanged;       // 文字列更新
            localizedFontAsset.AssetChanged   += OnFontAssetChanged;           // フォント更新 :contentReference[oaicite:0]{index=0}
            localizedMaterialAsset.AssetChanged += OnMaterialAssetChanged;     // マテリアル更新
        }

        private void Start()
        {
            // 最初の読み込みをトリガー
            localizedLanguageName.RefreshString();    // StringChanged が呼ばれる
            localizedFontAsset.LoadAssetAsync();      // AssetChanged が呼ばれる :contentReference[oaicite:1]{index=1}
            localizedMaterialAsset.LoadAssetAsync();  // 同様
        }

        private void OnDestroy()
        {
            // 購読解除
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            localizedLanguageName.StringChanged -= OnLanguageNameChanged;
            localizedFontAsset.AssetChanged   -= OnFontAssetChanged;
            localizedMaterialAsset.AssetChanged -= OnMaterialAssetChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            // ロケール変更時も再読み込み
            localizedLanguageName.RefreshString();
            localizedFontAsset.LoadAssetAsync();
            localizedMaterialAsset.LoadAssetAsync();
        }

        // ローカライズ済み言語名を受け取ってテキストを書き換え
        private void OnLanguageNameChanged(string localizedValue)
        {
            buttonTxt.text = localizedValue;
        }

        // ローカライズ済みフォントを受け取ってフォントを差し替え
        private void OnFontAssetChanged(TMP_FontAsset fontAsset)
        {
            if (fontAsset != null)
            {
                buttonTxt.font = fontAsset;
                buttonTxt.SetAllDirty();  // レイアウト再計算
            }
        }

        // （必要なら）ローカライズ済みマテリアルを受け取ってマテリアルを差し替え
        private void OnMaterialAssetChanged(Material mat)
        {
            if (mat != null)
                buttonTxt.fontMaterial = mat;
        }
    }
}