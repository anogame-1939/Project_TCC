using UnityEngine;
using System.Collections;

namespace AnoGame
{
    public class CinematicBars : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] RectTransform topBar;
        [SerializeField] RectTransform bottomBar;

        [Header("表示設定")]
        [SerializeField, Min(0f)] float barHeight = 140f;
        [SerializeField, Min(0f)] float duration = 0.35f;
        [SerializeField] bool useUnscaledTime = true;

        Coroutine _co;

        // ==== Public API ====
        public void Show() => StartAnim(barHeight);
        public void Hide() => StartAnim(0f);

        /// <summary>目標アスペクト比（例: 2.35f）に合わせて上下黒帯を計算して表示</summary>
        public void SetByAspect(float targetAspect)
        {
            float desiredH = Screen.width / Mathf.Max(0.0001f, targetAspect);
            float bar = Mathf.Max(0f, (Screen.height - desiredH) * 0.5f);
            StartAnim(bar);
        }

        // ==== Context Menu（コンポーネントの︙/右クリックから実行できます） ====
        [ContextMenu("Cinematic Bars/Show")]
        void Ctx_Show() => Show();

        [ContextMenu("Cinematic Bars/Hide")]
        void Ctx_Hide() => Hide();

        [ContextMenu("Cinematic Bars/Set by Aspect/Scope 2.39:1")]
        void Ctx_Scope239() => SetByAspect(2.39f);

        [ContextMenu("Cinematic Bars/Set by Aspect/Scope 2.35:1")]
        void Ctx_Scope235() => SetByAspect(2.35f);

        [ContextMenu("Cinematic Bars/Set by Aspect/Univisium 2.00:1")]
        void Ctx_Univisium200() => SetByAspect(2.00f);

        [ContextMenu("Cinematic Bars/Set by Aspect/Flat 1.85:1")]
        void Ctx_Flat185() => SetByAspect(1.85f);

        // ==== 内部処理 ====
        void StartAnim(float targetHeight)
        {
            // エディタ停止中は即時反映（コルーチンは回りにくいため）
            if (!UnityEngine.Application.isPlaying)
            {
                SetInstant(targetHeight);
                return;
            }

            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Animate(targetHeight));
        }

        void SetInstant(float h)
        {
            if (topBar != null)
            {
                var s = topBar.sizeDelta; s.y = h; topBar.sizeDelta = s;
            }
            if (bottomBar != null)
            {
                var s = bottomBar.sizeDelta; s.y = h; bottomBar.sizeDelta = s;
            }
        }

        IEnumerator Animate(float h)
        {
            float t = 0f, dur = Mathf.Max(0.0001f, duration);
            float startT = topBar ? topBar.sizeDelta.y : 0f;
            float startB = bottomBar ? bottomBar.sizeDelta.y : 0f;

            while (t < 1f)
            {
                t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
                float v = Mathf.Lerp(startT, h, t);

                if (topBar)
                {
                    var s = topBar.sizeDelta; s.y = v; topBar.sizeDelta = s;
                }
                if (bottomBar)
                {
                    var s = bottomBar.sizeDelta; s.y = v; bottomBar.sizeDelta = s;
                }
                yield return null;
            }
            SetInstant(h);
            _co = null;
        }
    }
}
