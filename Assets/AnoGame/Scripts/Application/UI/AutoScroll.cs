using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// UniTask を使って自動スクロールを行い、
    /// 終了後に指定した秒数だけ待ってから UnityEvent を呼び出します。
    /// </summary>
    public class AutoScroll : MonoBehaviour
    {
        [Header("スクロール設定")]
        [SerializeField] private ScrollRect scrollRect;         // ScrollRect 参照
        [SerializeField] private float scrollSpeed = 0.08f;      // スクロール速度

        [Header("スクロール開始")]
        [SerializeField, Tooltip("シーンスタート後、何秒後にスクロールを開始するか")]
        private float delayStart = 1.5f;

        [Header("完了通知設定")]
        [SerializeField, Tooltip("スクロール完了後、何秒待ってから通知するか")]
        private float delayAfterScroll = 1f;                  // スクロール完了後の待機時間

        [Header("完了イベント")]
        [SerializeField] private UnityEvent onScrollFinished;  // スクロール完了時に呼ばれる

        private void Start()
        {
            ScrollToEndAsync().Forget();
        }

        private async UniTaskVoid ScrollToEndAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delayStart));

            // MonoBehaviour が破棄されたら自動キャンセル
            var cancellation = this.GetCancellationTokenOnDestroy();

            // 1 → 0 に向けてスクロール
            while (scrollRect.verticalNormalizedPosition > 0f && !cancellation.IsCancellationRequested)
            {
                float next = scrollRect.verticalNormalizedPosition - scrollSpeed * Time.deltaTime;
                scrollRect.verticalNormalizedPosition = Mathf.Max(0f, next);

                // 次フレームまで待機
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
            }

            // スクロール終了後に delay 秒待つ
            if (delayAfterScroll > 0f && !cancellation.IsCancellationRequested)
            {
                // UniTask.Delay はミリ秒指定のオーバーロードもありますが、
                // TimeSpan での指定も可能です。
                await UniTask.Delay(TimeSpan.FromSeconds(delayAfterScroll), cancellationToken: cancellation);
            }

            // 最終的にイベント発火
            onScrollFinished?.Invoke();
        }
    }
}
