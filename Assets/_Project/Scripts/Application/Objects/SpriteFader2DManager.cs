using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnoGame.Application.Objects
{
    /// <summary>
    /// 複数の <see cref="SpriteFader2D"/> を一括でフェード操作するマネージャ。
    /// </summary>
    public class SpriteFader2DManager : MonoBehaviour
    {
        [SerializeField] SpriteFader2D[] faders;

        /// <summary>全フェーダーのアルファを即座に設定する。</summary>
        public void SetAlphaImmediate(float a)
        {
            if (faders == null) return;
            for (int i = 0; i < faders.Length; i++)
            {
                if (faders[i] != null) faders[i].SetAlphaImmediate(a);
            }
        }

        /// <summary>全フェーダーを一括フェードインさせる。</summary>
        public UniTask FadeIn(float? duration = null, CancellationToken ct = default)
        {
            return FadeAll(f => f.FadeIn(duration, ct));
        }

        /// <summary>全フェーダーを一括フェードアウトさせる。</summary>
        public UniTask FadeOut(float? duration = null, CancellationToken ct = default)
        {
            return FadeAll(f => f.FadeOut(duration, ct));
        }

        /// <summary>全フェーダーを一括で指定アルファへフェードさせる。</summary>
        public UniTask FadeTo(float dst, float duration, CancellationToken ct = default)
        {
            return FadeAll(f => f.FadeTo(dst, duration, ct));
        }

        /// <summary>全フェーダーを一括フェードインさせる（Fire-and-forget）。</summary>
        public void PlayFadeIn(float duration) => FadeIn(duration).Forget();

        /// <summary>全フェーダーを一括フェードアウトさせる（Fire-and-forget）。</summary>
        public void PlayFadeOut(float duration) => FadeOut(duration).Forget();

        /// <summary>全フェーダーを一括で指定アルファへフェードさせる（Fire-and-forget）。</summary>
        public void PlayFadeTo(float dst) => FadeTo(dst, 0.25f).Forget();

        UniTask FadeAll(System.Func<SpriteFader2D, UniTask> action)
        {
            if (faders == null || faders.Length == 0) return UniTask.CompletedTask;

            var tasks = new UniTask[faders.Length];
            for (int i = 0; i < faders.Length; i++)
            {
                tasks[i] = faders[i] != null ? action(faders[i]) : UniTask.CompletedTask;
            }
            return UniTask.WhenAll(tasks);
        }
    }
}
