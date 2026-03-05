using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine.SceneManagement;

namespace AnoGame.Application.Scene
{
    public static class SceneLoadExtensions
    {
        public static async UniTask LoadSceneAsyncStandard(
            string sceneName,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            CancellationToken ct = default)
        {
            // シーン読み込みを allowSceneActivation=false でキック
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            op.allowSceneActivation = false;

            // 0.9 以下の間、毎フレーム進捗を取得
            while (!ct.IsCancellationRequested && op.progress < 0.9f)
            {
                onProgress?.Invoke(op.progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }

            // 0.9 を超えた＝シーン読み込み完了直前
            onProgress?.Invoke(op.progress);

            // シーン切り替えを許可
            op.allowSceneActivation = true;

            // 完全に切り替わるまで待機（isDone が true になる）
            await UniTask.WaitUntil(() => op.isDone, cancellationToken: ct);

            // 最終的に 1.0 を通知
            onProgress?.Invoke(1f);
        }
    }
}