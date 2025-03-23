using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

using Cysharp.Threading.Tasks;
using System.Threading;


using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Codice.CM.Common.Encryption;

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

        // TODO:
        // フォントの設定はこれだけを使う？

        // 
        [SerializeField] private LocalizedTmpFont _localizedFontReference;
        public LocalizedTmpFont LocalizedFontReference => _localizedFontReference;

        // フォントテーブルのイベント処理
        public delegate void TextChanged(string text);
        public TextChanged TextChangedEvent;
        // フォントテーブルのイベント処理
        public delegate void FontChanged(TMP_FontAsset tmp_FontAsset);
        public FontChanged FontChangedEvent;

        public delegate void StringTableChanged(string message);
        public StringTableChanged StringTableChangedEvent;

        // public delegate void AssetTableChanged(Sprite flagSprite);
        // public AssetTableChanged AssetTableChangedEvent;


        // アクセス修飾子がprivateのstatic変数に生成したインスタンスを保存する
        private static LocalizationManager singleInstance = null;

        // インスタンスの取得はstaticプロパティもしくはstaticメソッドから行えるようにする
        // staticメソッドの場合
        public static LocalizationManager GetInstance()
        {
            if (singleInstance == null)
            {
                singleInstance = new LocalizationManager();
            }
            return singleInstance;
        }

        async void Start()
        {
            // ロケール変更時の処理を追加
            LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;
            
            // NOTE:TableChangedイベントは、SelectedLocaleChangedの後すぐに呼ばれる
            // テキスト変更のイベントを発火
            // _localizedStringTable.TableChanged += ChangeText;

            // フォント変更のイベントを発火
            if  (_fontTable != null)
            {
                _fontTable.TableChanged += ChangeFont;
            }

            // テキスト変更時のイベントを登録
            TextChangedEvent += OnTextChangedEvent;

            // フォント変更時のイベントを登録
            FontChangedEvent += OnFontChangedEvent;

            // TODO:いずれ消すこと
            // ビルド後、謎エラーで落ちてしまったため、リトライ処理を追加した
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    // テキストを翻訳する
                    ApplyLoclizedText().Forget();

                    // 対応するフォントを適用する
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
            // var localizedString = _localizedStringTable.GetLocalizedString();
            // TextChangedEvent?.Invoke(localizedString);
            // await ApplyLoclizedText();
        }

        /// <summary>
        /// ローカライズ設定に紐づいたアセットテーブルから
        /// フォントテーブルのエントリを取得する
        /// </summary>
        /// <param name="assetTable"></param>
        void ChangeFont(AssetTable assetTable)
        {
            if (assetTable == null)
            {
                Debug.LogError("フォントフォントのアセットテーブルが設定されていません。");
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


        /// <summary>
        /// 設定情報の有無をチェックする
        /// </summary>
        /// <returns></returns>
        private bool Validate()
        {
            // Stringテーブルはあるか
            if (_localizedStringTable == null)
            {

            }

            // fontテーブルはあるか
            if (_fontTable == null)
            {

            }

            return false;
        }

        void OnDestroy()
        {
            // ロケール変更時の処理を削除
            LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChanged;
        }

        /// <summary>
        /// 現在選択中のロケールを取得
        /// </summary>
        /// <returns></returns>
        public int GetCurrentLocaleIndex()
        {
            return LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
        }

        /// <summary>
        /// ロケールの変更処理
        /// 外部からロケールを変更する時に使用する
        /// </summary>
        /// <param name="index">該当ロケールのインデックス</param>
        public void ChangeLocale(int index)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[index];
            LocalizationSettings.SelectedLocale = locale;

            // LocaleIndexChangedEvent?.Invoke(index);
        }

        /// <summary>
        /// ロケール変更時の処理
        /// </summary>
        /// <param name="locale"></param>
        private async void SelectedLocaleChanged(Locale locale)
        {
            Debug.Log($"LocalizationManager:ロケール変更！{locale.Identifier},{locale.LocaleName}");

            UniTask.Void(async () =>
            {
                await ApplyLoclizedText();
            });
            // await ApplyFont();
        }

        /// <summary>
        /// 翻訳テキストを適用する
        /// </summary>
        /// <returns></returns>
        private async UniTask ApplyLoclizedText()
        {
            // すべてのTextMeshProUGUIに翻訳テキストを適用する
            var tmpros = Resources.FindObjectsOfTypeAll(typeof(TMP_Text)) as TMP_Text[];
            foreach(var tmpro in tmpros)
            {
                Debug.Log($"テキスト翻訳中:{tmpro.name}");
                LocalizeComponent localizeComponent = tmpro.gameObject.GetComponent<LocalizeComponent>();

                if(localizeComponent == null)
                {
                    if (localizeComponent.Ignore)
                    {
                        continue;
                    }
                    tmpro.gameObject.AddComponent<LocalizeComponent>();
                    localizeComponent = tmpro.gameObject.GetComponent<LocalizeComponent>();

                    // 改行を検知した場合は警告
                    if (tmpro.text.Contains("\n"))
                    {
                        Debug.LogWarning($"テキスト翻訳中に改行を検知しました。対象テキスト：'{tmpro.text}'");
                    }

                    localizeComponent.SetOriginText(tmpro.text);

                    string tableName = _localizedStringTable.TableReference.TableCollectionName;
                    var entry = LocalizationSettings.StringDatabase.GetTableEntry(tableName, localizeComponent.OriginText).Entry;
                    if (entry != null)
                    {
                        Debug.Log($"テキスト翻訳完了:{tmpro.text} -> {entry.LocalizedValue}");
                        tmpro.text = entry.LocalizedValue;
                    }
                }
            }
        }

        /// <summary>
        /// 翻訳したテキストを取得するためのメソッド
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        /// テーブルをプリロードする
        /// 不要かも
        /// </summary>
        /// <returns></returns>
        private async UniTask Preload()
        {
            // Preload string table.
            // await LocalizationSettings.StringDatabase.PreloadTables("String Table Name").Task;

            // Preload asset table.
            // await LocalizationSettings.AssetDatabase.PreloadTables("Asset Table Name").Task;
        }


        /// <summary>
        /// フォントを適用する
        /// </summary>
        /// <returns></returns>
        private async UniTask ApplyFont()
        {
            string tableName = _fontTable.TableReference.TableCollectionName;

            // 使ってないけど、テーブルのエントリーを取得する方法
            var entry = LocalizationSettings.AssetDatabase.GetTableEntry(tableName, FontTableKey).Entry;

            int currentLocaleIndex = LocalizationManager
                            .GetInstance()
                            .GetCurrentLocaleIndex();
            // TODO:_fontTableのインデックスを指定したい
            //var a = _fontTable.TableReference


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

