using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnoGame.Application.Objects
{
    [DisallowMultipleComponent]
    public class SpriteFader2D : MonoBehaviour
    {
        [SerializeField] bool includeChildren = true;
        [SerializeField, Min(0f)] float defaultDuration = 0.25f;
        [SerializeField] bool useUnscaledTime = false;

        SpriteRenderer[] _renderers;
        float _currentAlpha = 1f;

        void Awake()
        {
            _renderers = includeChildren
                ? GetComponentsInChildren<SpriteRenderer>(true)
                : new[] { GetComponent<SpriteRenderer>() };
        }

        public void SetAlphaImmediate(float a)
        {
            _currentAlpha = a;
            ApplyAlpha(a);
        }

        public UniTask FadeIn(float? duration = null, CancellationToken ct = default)
            => FadeTo(1f, duration ?? defaultDuration, ct);

        public UniTask FadeOut(float? duration = null, CancellationToken ct = default)
            => FadeTo(0f, duration ?? defaultDuration, ct);

        public async UniTask FadeTo(float dst, float duration, CancellationToken ct = default)
        {
            duration = Mathf.Max(0.0001f, duration);
            float src = _currentAlpha;
            float t = 0f;

            while (t < 1f)
            {
                ct.ThrowIfCancellationRequested();
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t = Mathf.Min(1f, t + dt / duration);
                _currentAlpha = Mathf.Lerp(src, dst, t);
                ApplyAlpha(_currentAlpha);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            ApplyAlpha(_currentAlpha);
        }

        void ApplyAlpha(float a)
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                var c = r.color;
                c.a = a;
                r.color = c;      // SpriteRenderer.color はインスタンス別ティントで安全
            }
        }
    }
}