using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading;
using AnoGame.Application.Core;
using AnoGame.Application.UI;

namespace AnoGame.Application.Scene
{
    /// <summary>
    /// フェードとNowLoading処理のラッパークラス
    /// シーン名を指定して呼び出すと、フェードとNowLoadingを画面に表示してからシーンを呼び出す
    /// </summary>
    public class SceneManager : SingletonMonoBehaviour<SceneManager>
    {
        [SerializeField]
        private float _defaltWaitTime = 3f;
        [SerializeField]
        private float _fadeInSpeed = 3f;
        [SerializeField]
        private float _fadeOutSpeed = 3f;

        // 起動時に無視するシーン名をキャッシュ
        private string _ignoreSceneName;

        private void Awake()
        {
            // 現在読み込まれているシーンを無視対象として保持
            _ignoreSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        public void LoadFirstScene(string sceneName)
        {
            var ct = this.GetCancellationTokenOnDestroy();

            UniTask.Void(async () =>
            {
                NowLoadingUI.Instance.ShowLoadingImage();

                // await UniTask.Delay(TimeSpan.FromSeconds(_defaltWaitTime), cancellationToken: ct);

                // FadeManager.Instance.FadeIn(0);

                await UniTask.Delay(TimeSpan.FromSeconds(_defaltWaitTime), cancellationToken: ct);

                await SceneLoadExtensions.LoadSceneAsyncStandard(sceneName, LoadSceneMode.Single);

                // NOTE:Steam向けの言語設定処理
                var lang = GetCurrentSceneIndex();
                Localizer.LocalizationManager.GetInstance().ChangeLocale(lang); 

                // シーン読み込み完了時に、無視対象＋新規シーン以外をアンロード
                // await UnloadOtherScenesAsync(sceneName, ct);

                NowLoadingUI.Instance.HideLoadingImage();

                FadeManager.Instance.FadeIn(_fadeOutSpeed);
            });
        }

        public int GetCurrentSceneIndex()
        {
            const int EN = 3; // 失敗時フォールバック（英語）

            string lang;
            try
            {
                lang = Steamworks.SteamApps.GetCurrentGameLanguage();
                if (string.IsNullOrWhiteSpace(lang)) return EN;
            }
            catch
            {
                return EN;
            }

            switch (lang.ToLowerInvariant())
            {
                // ---- ここから：あなたの14言語に直結させるケース ----

                case "schinese":         return 0;

                case "tchinese":         return 1;

                // English
                case "english":          return 2;

                // Finnish
                case "finnish":          return 3;

                // French
                case "french":           return 4;

                // German
                case "german":           return 5;

                // Indonesian
                case "indonesian":       return 7;

                // Italian
                case "italian":          return 8;

                // Japanese
                case "japanese":         return 9;

                // Korean（APIは "koreana"）
                case "koreana":          return 10;

                // Portuguese（どちらでも pt 扱いに寄せる）
                case "portuguese":
                case "brazilian":        return 11;

                // Russian
                case "russian":          return 12;

                // Spanish（LATAM も同じスロットへ寄せる）
                case "spanish":
                case "latam":            return 13;

                // ---- ここまで：14言語の直接対応 ----
                // 以降：API が返し得る他言語（未対応は英語へフォールバック）

                case "arabic":           return EN;
                case "bulgarian":        return EN;
                case "czech":            return EN;
                case "danish":           return EN;
                case "dutch":            return EN;
                case "greek":            return EN;
                case "hungarian":        return EN;
                case "norwegian":        return EN;
                case "polish":           return EN;
                case "romanian":         return EN;
                case "swedish":          return EN;
                case "thai":             return EN; // 必要なら 14言語に昇格させてOK
                case "turkish":          return EN;
                case "ukrainian":        return EN;
                case "vietnamese":       return EN;

                default:
                    return EN;
            }
        }

        /// <summary>
        /// フェードイン→シーン読み込み→他シーンをアンロードを実行
        /// </summary>
        public void LoadScene(string sceneName)
        {
            var ct = this.GetCancellationTokenOnDestroy();

            UniTask.Void(async () =>
            {
                FadeManager.Instance.FadeOut(_fadeInSpeed);

                await UniTask.Delay(TimeSpan.FromSeconds(_defaltWaitTime), cancellationToken: ct);

                NowLoadingUI.Instance.ShowLoadingImage();

                await UniTask.Delay(TimeSpan.FromSeconds(_defaltWaitTime), cancellationToken: ct);

                await SceneLoadExtensions.LoadSceneAsyncStandard(sceneName, LoadSceneMode.Single);

                // シーン読み込み完了時に、無視対象＋新規シーン以外をアンロード
                // await UnloadOtherScenesAsync(sceneName, ct);

                NowLoadingUI.Instance.HideLoadingImage();
                FadeManager.Instance.FadeIn(_fadeOutSpeed);
            });
        }

        /// <summary>
        /// 無視対象シーンと、引数で渡された新規シーン以外をアンロードする
        /// </summary>
        private async UniTask UnloadOtherScenesAsync(string newlyLoadedScene, CancellationToken ct)
        {
            int total = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = total - 1; i >= 0; i--)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) 
                    continue;

                var name = scene.name;
                // 無視対象 or 新規シーンならスキップ
                if (name == _ignoreSceneName || name == newlyLoadedScene)
                    continue;

                // アンロード
                var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                if (op != null)
                {
                    await op.ToUniTask(cancellationToken: ct);
                }
            }
        }
    }
}
