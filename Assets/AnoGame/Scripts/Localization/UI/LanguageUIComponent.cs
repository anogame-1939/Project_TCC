using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Localizer.UI
{
    public class LanguageUIComponent : MonoBehaviour
    {
        [Header("表示先の TextMeshPro")]
        [SerializeField] private TMP_Text buttonTxt;

        [Header("ローカライズ設定")]
        [SerializeField] private LocalizedString localizedLanguageName;             // 言語名用 String TableEntry
        [SerializeField] private LocalizedAsset<TMP_FontAsset> localizedFontAsset; // フォント用 Asset TableEntry
        [SerializeField] private LocalizedAsset<Material> localizedMaterialAsset;  // マテリアル用 Asset TableEntry

        private void Awake()
        {
            // ロケール切替時の処理を購読
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            // フォント／マテリアルは同じまま
            localizedFontAsset.AssetChanged     += OnFontAssetChanged;
            localizedMaterialAsset.AssetChanged += OnMaterialAssetChanged;
        }

        private void Start()
        {
            // 初回読み込み
            UpdateLanguageName();
            localizedFontAsset.LoadAssetAsync();
            localizedMaterialAsset.LoadAssetAsync();
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            localizedFontAsset.AssetChanged     -= OnFontAssetChanged;
            localizedMaterialAsset.AssetChanged -= OnMaterialAssetChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            // ロケールが変わったらテキストもフォントも再読み込み
            UpdateLanguageName();
            localizedFontAsset.LoadAssetAsync();
            localizedMaterialAsset.LoadAssetAsync();
        }

        // ★SelectedLocaleChanged のタイミングで取得・反映する
        private void UpdateLanguageName()
        {
            // 非同期でローカライズ文字列を取得
            AsyncOperationHandle<string> handle = localizedLanguageName.GetLocalizedStringAsync();
            handle.Completed += OnLanguageNameLoaded;
        }

        private void OnLanguageNameLoaded(AsyncOperationHandle<string> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                buttonTxt.text = handle.Result;
            }
            else
            {
                Debug.LogWarning($"言語名の取得に失敗しました: {handle.OperationException}");
            }
        }

        private void OnFontAssetChanged(TMP_FontAsset fontAsset)
        {
            if (fontAsset != null)
            {
                buttonTxt.font = fontAsset;
                buttonTxt.SetAllDirty();
            }
        }

        private void OnMaterialAssetChanged(Material mat)
        {
            if (mat != null)
            {
                buttonTxt.fontMaterial = mat;
            }
        }
    }
}
