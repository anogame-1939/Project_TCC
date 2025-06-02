using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Tables;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Localizer
{
    /// <summary>
    /// シーン上のTextMeshProUGUIに翻訳テキストを適用するクラス
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // リトライ回数
        private const int retryCount = 3;
        // リトライ間隔（ミリ秒）
        private const int retryDuration = 1000;

        public delegate void LocaleIndexChanged(int localeIndex);
        public LocaleIndexChanged LocaleIndexChangedEvent;

        [SerializeField] private LocalizedStringTable _localizedStringTable;
        public LocalizedStringTable LocalizedStringTable => _localizedStringTable;

        [SerializeField] private LocalizedAssetTable _fontTable;
        public LocalizedAssetTable FontTable => _fontTable;

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
                return;
            }

            // コルーチンの代わりに UniTask で非同期処理を呼び出す
            DebugAsync().Forget();
        }

        public static LocalizationManager GetInstance()
        {
            return singleInstance;
        }

        /// <summary>
        /// デバッグ用ユーティリティ。_localizedStringTable.TableReference が設定されるまで 1 秒ごとに待機し、
        /// 設定されたら翻訳処理・フォント適用を呼び出す。
        /// </summary>
        private async UniTaskVoid DebugAsync()
        {
            Debug.Log("DebugAsync Start");

            // _localizedStringTable とその TableReference.TableCollectionName が null でないまで待機
            while (true)
            {
                if (_localizedStringTable != null
                    && _localizedStringTable.TableReference != null
                    && !string.IsNullOrEmpty(_localizedStringTable.TableReference.TableCollectionName))
                {
                    Debug.Log($"DebugAsync: TableReference.TableCollectionName = {_localizedStringTable.TableReference.TableCollectionName}");
                    break;
                }
                else
                {
                    Debug.LogError("DebugAsync: _localizedStringTable または TableReference が null です。1秒待機して再チェックします。");
                }
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }

            Debug.Log("DebugAsync End: ローカライズ呼び出し開始");

            // 翻訳テキスト適用・フォント適用を実行（例外は無視して続行）
            try
            {
                await ApplyLocalizedTextWithRetry();
                await ApplyFontWithRetry();
            }
            catch (Exception e)
            {
                Debug.LogError($"DebugAsync: 翻訳適用またはフォント適用中に例外が発生しました: {e.Message}");
            }

            Debug.Log("DebugAsync End: ローカライズ呼び出し完了");
        }

        async void Start()
        {
            // テーブル参照のチェック
            if (_localizedStringTable == null)
            {
                Debug.LogError("LocalizedStringTable がインスペクターにアサインされていません。");
            }
            else if (string.IsNullOrEmpty(_localizedStringTable.TableReference.TableCollectionName))
            {
                Debug.LogError("LocalizedStringTable の TableReference が空です。インスペクターで確認してください。");
            }

            if (_fontTable == null)
            {
                Debug.LogError("FontTable がインスペクターにアサインされていません。");
            }
            else if (string.IsNullOrEmpty(_fontTable.TableReference.TableCollectionName))
            {
                Debug.LogError("FontTable の TableReference が空です。インスペクターで確認してください。");
            }

            // ロケール変更時の処理を追加
            LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;

            _localizedStringTable.TableChanged += ChangeText;
            if (_fontTable != null)
            {
                _fontTable.TableChanged += ChangeFont;
            }

            TextChangedEvent += OnTextChangedEvent;
            FontChangedEvent += OnFontChangedEvent;

            // Start メソッドでも翻訳・フォント適用を最初に試行（リトライ付き）
            try
            {
                await ApplyLocalizedTextWithRetry();
                await ApplyFontWithRetry();
            }
            catch (Exception e)
            {
                Debug.LogError($"Start: 初回翻訳適用またはフォント適用中に例外が発生しました: {e.Message}");
            }
        }

        /// <summary>
        /// 翻訳テキスト適用をリトライ付きで呼び出すヘルパー
        /// </summary>
        private async UniTask ApplyLocalizedTextWithRetry()
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await ApplyLocalizedText();
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ApplyLocalizedTextWithRetry: エラーが発生したためリトライします({i + 1}/{retryCount})。: {e.Message}");
                    await UniTask.Delay(retryDuration);
                }
            }
            Debug.LogError("ApplyLocalizedTextWithRetry: 最大リトライ回数に到達しました。");
        }

        /// <summary>
        /// フォント適用をリトライ付きで呼び出すヘルパー
        /// </summary>
        private async UniTask ApplyFontWithRetry()
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await ApplyFont();
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ApplyFontWithRetry: エラーが発生したためリトライします({i + 1}/{retryCount})。: {e.Message}");
                    await UniTask.Delay(retryDuration);
                }
            }
            Debug.LogError("ApplyFontWithRetry: 最大リトライ回数に到達しました。");
        }

        public async void ApplyLocalize()
        {
            // リトライ処理
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await ApplyLocalizedText();
                    await ApplyFont();
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ApplyLocalize: エラーが発生したためリトライします({i + 1}/{retryCount})。: {e.Message}");
                    await UniTask.Delay(retryDuration);
                }
            }
        }

        private void OnTextChangedEvent(string text)
        {
            Debug.Log($"テキストが変更されました。{text}");
        }

        private void OnFontChangedEvent(TMP_FontAsset fontAsset)
        {
            ApplyFont(fontAsset).Forget();
            Debug.Log($"フォントテーブルが変更されました。{fontAsset.name}");
        }

        async void ChangeText(StringTable stringTable)
        {
            // 必要に応じて実装
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
            Debug.Log($"LocalizationManager: ロケール変更！{locale.Identifier}, {locale.LocaleName}");
            await ApplyLocalizedTextWithRetry();
            await ApplyFontWithRetry();
        }

        /// <summary>
        /// すべての TMP_Text に対して、ローカライズされたテキストを適用する
        /// </summary>
        private async UniTask ApplyLocalizedText()
        {
            // Scene 上にあるすべての TMP_Text を検索
            var tmpros = Resources.FindObjectsOfTypeAll(typeof(TMP_Text)) as TMP_Text[];
            foreach (var tmpro in tmpros)
            {
                Debug.Log($"テキスト翻訳中: {tmpro.name} - {tmpro.text}");
                LocalizeComponent localizeComponent = tmpro.gameObject.GetComponent<LocalizeComponent>();

                if (localizeComponent == null)
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
                    Debug.Log($"テキスト翻訳スキップ: {tmpro.text}");
                    continue;
                }

                string tableName = _localizedStringTable.TableReference.TableCollectionName;
                var entry = LocalizationSettings.StringDatabase.GetTableEntry(tableName, localizeComponent.OriginText).Entry;
                if (entry != null)
                {
                    tmpro.text = entry.LocalizedValue;
                    Debug.Log($"テキスト翻訳完了: '{localizeComponent.OriginText}' -> '{entry.LocalizedValue}'");
                }
                else
                {
                    Debug.LogError($"対応する翻訳がありませんでした。:{tmpro.text}");
                }
            }

            Debug.Log("翻訳がすべて完了しました。");

            await UniTask.Yield(); // 必要に応じて一フレーム待機

            Debug.Log("翻訳がすべて完了しました。2");
        }

        /// <summary>
        /// システム設定からフォントテーブルを取得し、テキストに適用する（キーは _fontTableKey）。  
        /// 例では未実装ですので、必要に応じて TableEntryReference の選択ロジックを追加してください。
        /// </summary>
        private async UniTask ApplyFont()
        {
            Debug.Log("ApplyFont: メソッド開始");

            // ■ ① LocalizationSettings の初期化完了を待つ
            await LocalizationSettings.InitializationOperation.ToUniTask();

            // ■ ② TableReference がセットされているかチェック
            if (_fontTable == null || _localizedFontReference == null)
            {
                Debug.LogError("ApplyFont: _fontTable か _localizedFontReference が null です。Inspector を確認してください。");
                return;
            }

            Debug.Log($"ApplyFont: TableCollectionName = {_fontTable.TableReference.TableCollectionName}");

            // ■ ③ (書き換え) テーブルを同期的に取得する
            AssetTable assetTable = LocalizationSettings.AssetDatabase.GetTable(_fontTable.TableReference);
            if (assetTable == null)
            {
                // もしまだロードされていないなら、非同期で確実にロードする代替コードを走らせるか、
                // エラー扱いにしてしまっても構いません。
                Debug.LogError($"ApplyFont: AssetTable がまだロードされていません。TableReference: {_fontTable.TableReference.TableCollectionName}");
                return;
            }

            Debug.Log($"ApplyFont: AssetTable を取得できました: {assetTable.TableCollectionName}");

            // ■ ④ キー名を ResolveKeyName で取得 (デバッグ用途)
            string entryKey = _localizedFontReference.TableEntryReference.ResolveKeyName(assetTable.SharedData);
            if (string.IsNullOrEmpty(entryKey))
            {
                Debug.LogError($"ApplyFont: ResolveKeyName でエントリ名を解決できませんでした。(KeyId: {_localizedFontReference.TableEntryReference.KeyId})");
                return;
            }
            Debug.Log($"ApplyFont: (ResolveKeyName) Entry 名 = {entryKey}");

            // ■ ⑤ 取得した TableEntryReference をそのまま使ってフォントをロード
            Debug.Log($"ApplyFont: GetAssetAsync を呼び出します: KeyId={_localizedFontReference.TableEntryReference.KeyId}");
            AsyncOperationHandle<TMP_FontAsset> fontHandle =
                assetTable.GetAssetAsync<TMP_FontAsset>(_localizedFontReference.TableEntryReference);

            // ◆ ここを同期処理に置き換え
            // WaitForCompletion() を呼ぶと、その場でロードが完了するまでブロックします。
            // （※Editor の “Use Asset Database” モードでは即座に返ってくることが多いです）
            TMP_FontAsset tmpFontAsset = fontHandle.WaitForCompletion();

            if (tmpFontAsset == null)
            {
                Debug.LogError($"ApplyFont: フォントの同期ロードに失敗しました。(EntryReference ID: {_localizedFontReference.TableEntryReference.KeyId})");
                return;
            }
            Debug.Log($"ApplyFont: フォント '{tmpFontAsset.name}' を同期的にロードしました。");

            // ■ ⑥ シーン上のすべての TextMeshProUGUI に適用
            var tmpros = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (var tmpro in tmpros)
            {
                var localizeComponent = tmpro.GetComponent<LocalizeComponent>();
                if (localizeComponent != null && localizeComponent.Ignore)
                    continue;
                tmpro.font = tmpFontAsset;
            }
            Debug.Log($"ApplyFont: 全 TextMeshProUGUI にフォント '{tmpFontAsset.name}' を適用しました");
        }


        private async UniTaskVoid ApplyFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                Debug.LogError("ApplyFont(TMP_FontAsset): 渡されたフォントアセットが null です。");
                return;
            }

            // シーンにあるすべての TextMeshProUGUI を取得し、フォントを置き換え
            var tmpros = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (var tmpro in tmpros)
            {
                var localizeComponent = tmpro.GetComponent<LocalizeComponent>();
                if (localizeComponent != null && localizeComponent.Ignore)
                {
                    continue;
                }
                tmpro.font = fontAsset;
            }

            // 必要であれば一フレーム待つ 
            await UniTask.Yield();
        }

        /// <summary>
        /// 特定のキーに対応する翻訳テキストを取得する
        /// </summary>
        public async UniTask<string> GetLocalizedText(string key)
        {
            string tableName = _localizedStringTable.TableReference.TableCollectionName;
            var entry = LocalizationSettings.StringDatabase.GetTableEntry(tableName, key).Entry;

            if (entry == null)
            {
                Debug.LogError($"翻訳対象のテキストが翻訳テーブルのキーに登録されていません。テーブル: {tableName}, キー: {key}");
                return key;
            }
            return entry.Value;
        }

        private async UniTask Preload()
        {
            // 必要に応じたプリロード処理
            await UniTask.CompletedTask;
        }
    }
}
