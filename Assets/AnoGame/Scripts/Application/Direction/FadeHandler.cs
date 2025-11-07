using AnoGame.Application.UI;
using UnityEngine;

namespace AnoGame.Application.Direction
{
    [DisallowMultipleComponent]
    public class FadeHandler : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _defaultDuration = 0.5f;

        public void FadeIn()               => FadeIn(_defaultDuration);
        public void FadeOut()              => FadeOut(_defaultDuration);
        public void FadeOutIn()            => FadeOutIn(_defaultDuration);

        public void FadeIn(float duration)
        {
            // ← ここをあなたの実装に合わせて差し替え
            // 例) FadeManager 利用
            FadeManager.Instance?.FadeIn(duration);

            // 例) 直接 FadeImage を操作するならここでコルーチンやTweenを開始
            // TODO: Direct fade impl if needed
        }

        public void FadeOut(float duration)
        {
            FadeManager.Instance?.FadeOut(duration);
            // TODO: Direct fade impl if needed
        }

        public void FadeOutIn(float duration)
        {
            FadeManager.Instance?.FadeOutIn(duration);
            // TODO: Direct fade impl if needed
        }
    }
}
