using AnoGame.Application.UI;
using UnityEngine;

namespace AnoGame.Application.Direction
{
    [DisallowMultipleComponent]
    public class FadeHandler : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _defaultDuration = 0.5f;

        public void FadeIn() => FadeIn(_defaultDuration);
        public void FadeOut() => FadeOut(_defaultDuration);
        public void FadeOutIn() => FadeOutIn(_defaultDuration);

        /// <summary>
        /// Timeline の ProcessFrame から直接 Range 値を設定する。
        /// コルーチンを使わず、即座に反映される。
        /// </summary>
        /// <param name="range">0 = 透明（画面表示）/ 1 = 暗転（フェードアウト）</param>
        public void SetFadeRange(float range, Color? color = null)
        {
            if (FadeManager.Instance == null) return;
            FadeManager.Instance.SetRange(range, color);
        }

        public void FadeIn(float duration, Color? color = null)
        {
            // ← ここをあなたの実装に合わせて差し替え
            // 例) FadeManager 利用
            FadeManager.Instance?.FadeIn(duration, color);

            // 例) 直接 FadeImage を操作するならここでコルーチンやTweenを開始
            // TODO: Direct fade impl if needed
        }

        public void FadeOut(float duration, Color? color = null)
        {
            FadeManager.Instance?.FadeOut(duration, color);
            // TODO: Direct fade impl if needed
        }

        public void FadeOutIn(float duration, Color? color = null)
        {
            FadeManager.Instance?.FadeOutIn(duration, color);
            // TODO: Direct fade impl if needed
        }
    }
}
