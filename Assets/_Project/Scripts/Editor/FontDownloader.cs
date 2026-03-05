using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AnoGame.Editor
{
    /// <summary>
    /// Fontsフォルダが空の場合、Googleドライブからフォントをダウンロードするユーティリティ。
    /// 自動DLに失敗した場合はブラウザでDLページを開くフォールバックあり。
    /// </summary>
    [InitializeOnLoad]
    internal static class FontDownloader
    {
        /// <summary>GoogleドライブのZIPファイルID</summary>
        private const string GoogleDriveFileId = "1c6pqLNGqjs1fvqEpGfgdN0ja8CrtTRXD";

        /// <summary>ブラウザフォールバック用URL（フォルダ共有リンク）</summary>
        private const string GoogleDriveFolderUrl =
            "https://drive.google.com/drive/folders/13GJEWQex8BqeE-Ah8Vwwc1P9eawLAX9f?usp=sharing";

        /// <summary>フォントフォルダのプロジェクト相対パス</summary>
        private const string FontsRelativePath = "Assets/_Project/Fonts";

        /// <summary>一度のEditorセッションで多重実行しないためのフラグ</summary>
        private static bool s_checked;

        static FontDownloader()
        {
            if (s_checked) return;
            s_checked = true;

            // Editorの初期化完了後に実行
            EditorApplication.delayCall += CheckAndPrompt;
        }

        [MenuItem("Tools/AnoGame/Download Fonts")]
        private static void DownloadFontsMenu()
        {
            if (HasFontFiles())
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "フォントダウンロード",
                    "Fontsフォルダにはすでにファイルが存在します。\n上書きダウンロードしますか？",
                    "上書きする",
                    "キャンセル");

                if (!overwrite) return;
            }

            ExecuteDownload();
        }

        /// <summary>
        /// Fontsフォルダが空かチェックし、空ならダウンロードを促す。
        /// </summary>
        private static void CheckAndPrompt()
        {
            if (HasFontFiles()) return;

            bool result = EditorUtility.DisplayDialog(
                "フォントが見つかりません",
                "Fontsフォルダにフォントファイルが存在しません。\n" +
                "Googleドライブからフォントをダウンロードしますか？\n\n" +
                "※ 自動ダウンロードに失敗した場合、ブラウザでダウンロードページを開きます。",
                "ダウンロード",
                "後で");

            if (!result) return;

            ExecuteDownload();
        }

        /// <summary>
        /// Fontsフォルダ内にフォントファイル（.ttf, .otf, .asset）が存在するかチェック。
        /// </summary>
        private static bool HasFontFiles()
        {
            string fullPath = Path.GetFullPath(FontsRelativePath);
            if (!Directory.Exists(fullPath)) return false;

            string[] fontExtensions = { "*.ttf", "*.otf", "*.asset" };
            foreach (string ext in fontExtensions)
            {
                if (Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories).Length > 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// ダウンロード処理のエントリポイント。自動DLを試み、失敗時はブラウザフォールバック。
        /// </summary>
        private static void ExecuteDownload()
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "ProjectFonts_" + Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                EditorUtility.DisplayProgressBar("フォントダウンロード", "Googleドライブからダウンロード中...", 0.1f);

                bool success = DownloadFromGoogleDrive(GoogleDriveFileId, tempZipPath);

                if (!success)
                {
                    EditorUtility.ClearProgressBar();
                    FallbackToBrowser();
                    return;
                }

                EditorUtility.DisplayProgressBar("フォントダウンロード", "ZIPを展開中...", 0.7f);

                ExtractZipToFonts(tempZipPath);

                EditorUtility.DisplayProgressBar("フォントダウンロード", "アセットデータベースを更新中...", 0.9f);

                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "フォントダウンロード完了",
                    "フォントファイルのダウンロードと展開が完了しました。",
                    "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[FontDownloader] ダウンロード中にエラーが発生しました: {ex.Message}\n{ex.StackTrace}");
                FallbackToBrowser();
            }
            finally
            {
                // 一時ファイルの削除
                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); }
                    catch { /* 無視 */ }
                }
            }
        }

        /// <summary>
        /// Googleドライブからファイルをダウンロードする。
        /// 大容量ファイルの確認ページにも対応。
        /// </summary>
        private static bool DownloadFromGoogleDrive(string fileId, string destPath)
        {
            string baseUrl = $"https://drive.google.com/uc?export=download&id={fileId}";

            // CookieContainerを使って確認トークンを通す
            var cookieContainer = new CookieContainer();
            var handler = new System.Net.Http.HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AllowAutoRedirect = true,
                UseCookies = true,
            };

            using (var client = new System.Net.Http.HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(10);

                // 1回目のリクエスト: 小さいファイルなら直接DL、大きいファイルなら確認ページ
                var response = client.GetAsync(baseUrl).GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"[FontDownloader] 初回リクエスト失敗: {response.StatusCode}");
                    return false;
                }

                string contentType = response.Content.Headers.ContentType?.MediaType ?? "";

                // ZIPが直接返ってきた場合
                if (contentType.Contains("application/zip") ||
                    contentType.Contains("application/octet-stream") ||
                    contentType.Contains("application/x-zip"))
                {
                    EditorUtility.DisplayProgressBar("フォントダウンロード", "ファイルを保存中...", 0.4f);
                    SaveResponseToFile(response, destPath);
                    return true;
                }

                // 確認ページが返ってきた場合 — confirmトークンを取得して再リクエスト
                string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                string confirmUrl = ExtractConfirmUrl(html, fileId);
                if (string.IsNullOrEmpty(confirmUrl))
                {
                    Debug.LogWarning("[FontDownloader] 確認トークンを取得できませんでした。");
                    return false;
                }

                EditorUtility.DisplayProgressBar("フォントダウンロード", "確認済みダウンロード中...", 0.3f);

                var confirmResponse = client.GetAsync(confirmUrl).GetAwaiter().GetResult();

                if (!confirmResponse.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"[FontDownloader] 確認DLリクエスト失敗: {confirmResponse.StatusCode}");
                    return false;
                }

                EditorUtility.DisplayProgressBar("フォントダウンロード", "ファイルを保存中...", 0.5f);
                SaveResponseToFile(confirmResponse, destPath);
                return true;
            }
        }

        /// <summary>
        /// 確認ページHTMLからダウンロードURLを抽出する。
        /// </summary>
        private static string ExtractConfirmUrl(string html, string fileId)
        {
            // パターン1: form action からURLを取得
            var actionMatch = Regex.Match(html, @"action=""([^""]+)""", RegexOptions.IgnoreCase);
            if (actionMatch.Success)
            {
                string actionUrl = WebUtility.HtmlDecode(actionMatch.Groups[1].Value);

                // 相対URLの場合
                if (actionUrl.StartsWith("/"))
                    actionUrl = "https://drive.google.com" + actionUrl;

                // id, export, confirmパラメータの取得
                var idMatch = Regex.Match(html, @"name=""id""\s+value=""([^""]+)""");
                var exportMatch = Regex.Match(html, @"name=""export""\s+value=""([^""]+)""");
                var confirmMatch = Regex.Match(html, @"name=""confirm""\s+value=""([^""]+)""");
                var uuidMatch = Regex.Match(html, @"name=""uuid""\s+value=""([^""]+)""");

                string queryParams = "";
                if (idMatch.Success) queryParams += $"&id={idMatch.Groups[1].Value}";
                if (exportMatch.Success) queryParams += $"&export={exportMatch.Groups[1].Value}";
                if (confirmMatch.Success) queryParams += $"&confirm={confirmMatch.Groups[1].Value}";
                if (uuidMatch.Success) queryParams += $"&uuid={uuidMatch.Groups[1].Value}";

                if (queryParams.Length > 0)
                {
                    string separator = actionUrl.Contains("?") ? "&" : "?";
                    return actionUrl + separator + queryParams.TrimStart('&');
                }

                return actionUrl;
            }

            // パターン2: confirmパラメータだけ取得して元のURLに付与
            var simpleConfirmMatch = Regex.Match(html, @"confirm=([0-9A-Za-z_-]+)");
            if (simpleConfirmMatch.Success)
            {
                string token = simpleConfirmMatch.Groups[1].Value;
                return $"https://drive.google.com/uc?export=download&confirm={token}&id={fileId}";
            }

            return null;
        }

        /// <summary>
        /// HTTPレスポンスをファイルに保存する。
        /// </summary>
        private static void SaveResponseToFile(System.Net.Http.HttpResponseMessage response, string destPath)
        {
            string dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// ZIPをFontsフォルダに展開する。
        /// ZIP内のルートフォルダ構造を検出し、適切にマッピングする。
        /// </summary>
        private static void ExtractZipToFonts(string zipPath)
        {
            string fontsFullPath = Path.GetFullPath(FontsRelativePath);

            if (!Directory.Exists(fontsFullPath))
                Directory.CreateDirectory(fontsFullPath);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                // ZIPのルートフォルダ名を検出（例: "Fonts/" が含まれている場合、それを除去）
                string rootPrefix = DetectZipRootPrefix(archive);

                foreach (var entry in archive.Entries)
                {
                    // ディレクトリエントリはスキップ
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    string entryPath = entry.FullName;

                    // ルートプレフィックスを除去
                    if (!string.IsNullOrEmpty(rootPrefix) && entryPath.StartsWith(rootPrefix))
                        entryPath = entryPath.Substring(rootPrefix.Length);

                    // 空になった場合はスキップ
                    if (string.IsNullOrEmpty(entryPath)) continue;

                    // パス区切りを統一
                    entryPath = entryPath.Replace('/', Path.DirectorySeparatorChar);

                    string destFilePath = Path.Combine(fontsFullPath, entryPath);
                    string destDir = Path.GetDirectoryName(destFilePath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    entry.ExtractToFile(destFilePath, overwrite: true);
                }
            }
        }

        /// <summary>
        /// ZIP内のルートフォルダプレフィックスを検出する。
        /// 例: 全エントリが "Fonts/..." で始まっていれば "Fonts/" を返す。
        /// </summary>
        private static string DetectZipRootPrefix(ZipArchive archive)
        {
            string commonPrefix = null;

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                int slashIndex = entry.FullName.IndexOf('/');
                if (slashIndex < 0) return ""; // ルートフォルダなし

                string prefix = entry.FullName.Substring(0, slashIndex + 1);

                if (commonPrefix == null)
                    commonPrefix = prefix;
                else if (commonPrefix != prefix)
                    return ""; // 複数のルートフォルダ → プレフィックスなし
            }

            return commonPrefix ?? "";
        }

        /// <summary>
        /// 自動DL失敗時のフォールバック: ブラウザでGoogleドライブを開く。
        /// </summary>
        private static void FallbackToBrowser()
        {
            bool open = EditorUtility.DisplayDialog(
                "自動ダウンロードに失敗しました",
                "自動ダウンロードに失敗しました。\n" +
                "ブラウザでGoogleドライブを開きますので、\n" +
                "手動でダウンロード後、ZIPを展開して\n" +
                "Assets/_Project/Fonts フォルダに配置してください。",
                "ブラウザを開く",
                "閉じる");

            if (open)
            {
                UnityEngine.Application.OpenURL(GoogleDriveFolderUrl);
            }
        }
    }
}
