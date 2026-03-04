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
        [SerializeField] private LocalizedAsset<TMP_FontAsset> localizedFontAsset;  // フォント用 Asset TableEntry
        [SerializeField] private LocalizedAsset<Material> localizedMaterialAsset;   // マテリアル用 Asset TableEntry

        private void Awake()
        {
            // ロケールが切り替わったら UpdateVisuals() を呼ぶ
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void Start()
        {
            // 最初の一回
            UpdateVisuals();
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            // ロケールごとにテキスト／フォント／マテリアルを再読み込み
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 1) 言語名テキスト
            var textHandle = localizedLanguageName.GetLocalizedStringAsync();
            textHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    buttonTxt.text = handle.Result;
                }
                else
                {
                    Debug.LogWarning($"言語名取得失敗: {handle.OperationException}");
                }
            };

            // 2) フォント
            var fontHandle = localizedFontAsset.LoadAssetAsync();
            fontHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    buttonTxt.font = handle.Result;
                    buttonTxt.SetAllDirty();  // レイアウト再計算
                }
                else
                {
                    Debug.LogWarning($"フォント読み込み失敗: {handle.OperationException}");
                }
            };

            // 3) マテリアル
            /* NOTE:現状使ってない
            var matHandle = localizedMaterialAsset.LoadAssetAsync();
            matHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    buttonTxt.fontMaterial = handle.Result;
                }
                else
                {
                    Debug.LogWarning($"マテリアル読み込み失敗: {handle.OperationException}");
                }
            };
            */
        }
    }
}
