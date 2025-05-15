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

                // シーン読み込み完了時に、無視対象＋新規シーン以外をアンロード
                // await UnloadOtherScenesAsync(sceneName, ct);

                NowLoadingUI.Instance.HideLoadingImage();

                FadeManager.Instance.FadeIn(_fadeOutSpeed);
            });
        }

        /// <summary>
        /// フェードイン→シーン読み込み→他シーンをアンロードを実行
        /// </summary>
        public void LoadScene(string sceneName)
        {
            var ct = this.GetCancellationTokenOnDestroy();

            UniTask.Void(async () =>
            {
                NowLoadingUI.Instance.ShowLoadingImage();

                await UniTask.Delay(TimeSpan.FromSeconds(_defaltWaitTime), cancellationToken: ct);

                FadeManager.Instance.FadeIn(_fadeInSpeed);

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
