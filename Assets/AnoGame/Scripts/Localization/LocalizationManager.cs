using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Tables;
using System.Collections;

namespace Localizer
{
    /// <summary>
    /// シーン上のTextMeshProUGUIに翻訳テキストを適用するクラス
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // リトライ回数
        private const int retryCount = 3;
        // リトライ間隔
        private const int retryDuration = 1000;

        public delegate void LocaleIndexChanged(int localeIndex);
        public LocaleIndexChanged LocaleIndexChangedEvent;

        [SerializeField] private LocalizedStringTable _localizedStringTable;
        public LocalizedStringTable LocalizedStringTable => _localizedStringTable;

        [SerializeField] private LocalizedAssetTable _fontTable;
        public LocalizedAssetTable FontTable => _fontTable;

        [SerializeField] private string _fontTableKey = "Font";
        public string FontTableKey => _fontTableKey;

        [SerializeField] private LocalizedTmpFont _localizedFontReference;
        public LocalizedTmpFont LocalizedFontReference => _localizedFontReference;

        // フォントテーブルのイベント処理
        public delegate void TextChanged(string text);
        public TextChanged TextChangedEvent;
        public delegate void FontChanged(TMP_FontAsset tmp_FontAsset);
        public FontChanged FontChangedEvent;
        public delegate void StringTableChanged(string message);
        public StringTableChanged StringTableChangedEvent;

        // シングルトン用
        private static LocalizationManager singleInstance = null;

        void Awake()
        {
            if (singleInstance == null)
            {
                singleInstance = this;
                // シーン遷移時にも残す場合はコメント解除
                // DontDestroyOnLoad(gameObject);
            }
            else if (singleInstance != this)
            {
                Destroy(gameObject);
            }
            StartCoroutine(DebugCor());

        }

        public static LocalizationManager GetInstance()
        {
            return singleInstance;
        }

        private IEnumerator DebugCor()
        {

            Debug.Log("DebugCor Start");
            while (true)
            {
                Debug.Log($"DebugCor Loop _localizedStringTable:{_localizedStringTable}");
                Debug.Log($"DebugCor Loop _fontTable:{_fontTable}");
                Debug.Log($"DebugCor Loop _localizedFontReference:{_localizedFontReference}");
                yield return new WaitForSeconds(1f);
            }

        }

        async void Start()
        {
            // テーブル参照のチェック
            if (_localizedStringTable == null)
            {
                Debug.LogError("LocalizedStringTableがインスペクターにアサインされていません。");
            }
            else if (string.IsNullOrEmpty(_localizedStringTable.TableReference.TableCollectionName))
            {
                Debug.LogError("LocalizedStringTableのTableReferenceが空です。インスペクターで確認してください。");
            }

            if (_fontTable == null)
            {
                Debug.LogError("FontTableがインスペクターにアサインされていません。");
            }
            else if (string.IsNullOrEmpty(_fontTable.TableReference.TableCollectionName))
            {
                Debug.LogError("FontTableのTableReferenceが空です。インスペクターで確認してください。");
            }

            // ロケール変更時の処理を追加
            LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;
            
            // _localizedStringTable.TableChanged += ChangeText;
            if (_fontTable != null)
            {
                _fontTable.TableChanged += ChangeFont;
            }

            TextChangedEvent += OnTextChangedEvent;
            FontChangedEvent += OnFontChangedEvent;

            // リトライ処理
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    ApplyLoclizedText().Forget();
                    ApplyFont().Forget();
                    break;
                }
                catch (System.Exception e)
                {
                    Debug.Log($"エラーが発生したため、リトライします。:{e.Message}");
                    await UniTask.Delay(retryDuration);
                    continue;
                }
            }
        }

        private void OnTextChangedEvent(string text)
        {
            Debug.Log($"テキストが変更されました。{text}");
        }

        private void OnFontChangedEvent(TMP_FontAsset fontAsset)
        {
            ApplyFont(fontAsset);
            Debug.Log($"フォントテーブルが変更されました。{fontAsset.name}");
        }
        
        async void ChangeText(StringTable stringTable)
        {
            // 例：テーブル変更時の処理（必要に応じて実装）
        }

        void ChangeFont(AssetTable assetTable)
        {
            if (assetTable == null)
            {
                Debug.LogError("フォントのアセットテーブルが設定されていません。");
                return;
            }
            if (_localizedFontReference == null)
            {
                Debug.LogError("フォントテーブルが設定されていません。");
                return;
            }
            var operation = assetTable.GetAssetAsync<TMP_FontAsset>(_localizedFontReference.TableEntryReference);
            operation.Completed += handle => FontChangedEvent?.Invoke(handle.Result);
        }

        private bool Validate()
        {
            if (_localizedStringTable == null)
            {
                // 必要なエラー処理を追加
            }
            if (_fontTable == null)
            {
                // 必要なエラー処理を追加
            }
            return false;
        }

        void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChanged;
        }

        public int GetCurrentLocaleIndex()
        {
            return LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
        }

        public void ChangeLocale(int index)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[index];
            LocalizationSettings.SelectedLocale = locale;
            // LocaleIndexChangedEvent?.Invoke(index);
        }

        private async void SelectedLocaleChanged(Locale locale)
        {
            Debug.Log($"LocalizationManager:ロケール変更！{locale.Identifier}, {locale.LocaleName}");
            UniTask.Void(async () =>
            {
                await ApplyLoclizedText();
            });
        }

        private async UniTask ApplyLoclizedText()
        {
            var tmpros = Resources.FindObjectsOfTypeAll(typeof(TMP_Text)) as TMP_Text[];
            foreach(var tmpro in tmpros)
            {
                Debug.Log($"テキスト翻訳中:{tmpro.name}");
                LocalizeComponent localizeComponent = tmpro.gameObject.GetComponent<LocalizeComponent>();

                if(localizeComponent == null)
                {
                    tmpro.gameObject.AddComponent<LocalizeComponent>();
                    localizeComponent = tmpro.gameObject.GetComponent<LocalizeComponent>();

                    if (tmpro.text.Contains("\n"))
                    {
                        Debug.LogWarning($"テキスト翻訳中に改行を検知しました。対象テキスト：'{tmpro.text}'");
                    }
                    localizeComponent.SetOriginText(tmpro.text);
                }
                else if (localizeComponent.Ignore)
                {
                    Debug.Log($"テキスト翻訳スキップ:{tmpro.text}");
                    continue;
                }

                string tableName = _localizedStringTable.TableReference.TableCollectionName;
                var entry = LocalizationSettings.StringDatabase.GetTableEntry(tableName, localizeComponent.OriginText).Entry;
                if (entry != null)
                {
                    Debug.Log($"テキスト翻訳完了:{tmpro.text} -> {entry.LocalizedValue}");
                    tmpro.text = entry.LocalizedValue;
                }
            }
        }

        public async UniTask<string> GetLocalizedText(string key)
        {
            string tableName = _localizedStringTable.TableReference.TableCollectionName;
            var entry = LocalizationSettings.StringDatabase.GetTableEntry(tableName, key).Entry;

            if (entry == null)
            {
                Debug.LogError($"翻訳対象のテキストが翻訳テーブルのキーに登録されていません。テーブル:{tableName}, キー:{key}");
                return key;
            }
            return entry.Value;
        }

        private async UniTask Preload()
        {
            // 必要に応じたプリロード処理
        }

        private async UniTask ApplyFont()
        {
            string tableName = _fontTable.TableReference.TableCollectionName;
            var entry = LocalizationSettings.AssetDatabase.GetTableEntry(tableName, FontTableKey).Entry;
            int currentLocaleIndex = LocalizationManager.GetInstance().GetCurrentLocaleIndex();
            // TODO: _fontTable のインデックス指定などの処理を実装する
        }

        private async UniTask ApplyFont(TMP_FontAsset tmpFontAsset)
        {
            var tmpros = Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI)) as TextMeshProUGUI[];
            foreach(var tmpro in tmpros)
            {
                tmpro.font = tmpFontAsset;
            }
        }
    }
}
