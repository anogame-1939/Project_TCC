using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using AnoGame.Application.Core;

namespace AnoGame.Application.UI
{
    public class FadeManager : SingletonMonoBehaviour<FadeManager>
    {
        [SerializeField]
        private FadeImage _fadeImage;

        [Header("オンなら画面を表示。オフなら暗転して非表示。")]
        [SerializeField]
        private bool defaultFeedIn = false;

        private Coroutine _fadeCoroutine;

        void Start()
        {
            Assert.IsNotNull(_fadeImage, "FadeImage component is not assigned");
            if (defaultFeedIn) FadeIn(0);
            else FadeOut(0);
        }

        /// <summary>
        /// Timeline から直接呼ばれる。進行中のコルーチンを停止し、即座に Range を設定する。
        /// </summary>
        public void SetRange(float range, Color? color = null)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            if (color.HasValue) _fadeImage.SetColor(color.Value);
            _fadeImage.Range = Mathf.Clamp01(range);
        }

        /// <summary>
        /// 指定した秒数かけてフェードインを行います
        /// </summary>
        /// <param name="duration">フェードにかかる時間(秒)</param>
        public void FadeOutIn(float duration, Color? color = null)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            if (color.HasValue) _fadeImage.SetColor(color.Value);
            _fadeCoroutine = StartCoroutine(FadeOutInRoutine(duration));
        }

        private IEnumerator FadeOutInRoutine(float duration)
        {
            var halfDuration = duration / 2;
            yield return FadeRoutine(0f, 1f, halfDuration);

            yield return new WaitForSeconds(halfDuration / 2);

            yield return FadeRoutine(1f, 0f, halfDuration);

            _fadeCoroutine = null;
        }

        /// <summary>
        /// 指定した秒数かけてフェードインを行います
        /// </summary>
        /// <param name="duration">フェードにかかる時間(秒)</param>
        public void FadeIn(float duration, Color? color = null)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            if (color.HasValue) _fadeImage.SetColor(color.Value);
            _fadeCoroutine = StartCoroutine(FadeInRoutine(duration));
        }

        private IEnumerator FadeInRoutine(float duration)
        {
            yield return FadeRoutine(1f, 0f, duration);
            _fadeCoroutine = null;
        }

        /// <summary>
        /// 指定した秒数かけてフェードアウトを行います
        /// </summary>
        /// <param name="duration">フェードにかかる時間(秒)</param>
        public void FadeOut(float duration, Color? color = null)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            if (color.HasValue) _fadeImage.SetColor(color.Value);
            _fadeCoroutine = StartCoroutine(FadeOutRoutine(duration));
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            yield return FadeRoutine(0f, 1f, duration);
            _fadeCoroutine = null;
        }

        private IEnumerator FadeRoutine(float startRange, float endRange, float duration)
        {
            float elapsed = 0f;
            _fadeImage.Range = startRange;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                _fadeImage.Range = Mathf.Lerp(startRange, endRange, normalizedTime);
                yield return null;
            }

            _fadeImage.Range = endRange;
        }
    }
}