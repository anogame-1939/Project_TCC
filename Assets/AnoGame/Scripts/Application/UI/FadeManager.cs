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
        /// 指定した秒数かけてフェードインを行います
        /// </summary>
        /// <param name="duration">フェードにかかる時間(秒)</param>
        public void FadeOutIn(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
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
        public void FadeIn(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
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
        public void FadeOut(float duration)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
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