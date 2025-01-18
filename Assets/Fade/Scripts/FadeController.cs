using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

public class FadeImageController : MonoBehaviour
{
    [SerializeField]
    private FadeImage fadeImage;

    private Coroutine fadeCoroutine;

    void Start()
    {
        Assert.IsNotNull(fadeImage, "FadeImage component is not assigned");
		FadeIn(0);
    }

    /// <summary>
    /// 指定した秒数かけてフェードインを行います
    /// </summary>
    /// <param name="duration">フェードにかかる時間(秒)</param>
    public void FadeIn(float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0f, duration));
    }

    /// <summary>
    /// 指定した秒数かけてフェードアウトを行います
    /// </summary>
    /// <param name="duration">フェードにかかる時間(秒)</param>
    public void FadeOut(float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    private IEnumerator FadeRoutine(float startRange, float endRange, float duration)
    {
        float elapsed = 0f;
        fadeImage.Range = startRange;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            fadeImage.Range = Mathf.Lerp(startRange, endRange, normalizedTime);
            yield return null;
        }

        fadeImage.Range = endRange;
        fadeCoroutine = null;
    }
}