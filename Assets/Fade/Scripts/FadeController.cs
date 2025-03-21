using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using PixelCrushers.DialogueSystem;

public class FadeImageController : MonoBehaviour
{
    [SerializeField]
    private FadeImage _fadeImage;

    [Header("オンなら画面を表示。オフなら暗転して非表示。")]
    [SerializeField]
    private bool defalutFeedIn = false;

    private Coroutine _fadeCoroutine;

	private DialogueSystemTrigger _dialogueSystemTrigger;

    void Start()
    {
        Assert.IsNotNull(_fadeImage, "FadeImage component is not assigned");
		if (defalutFeedIn) FadeIn(0);
        else FadeOut(0);
    }

	public void SetDialogueSystemTrigger(DialogueSystemTrigger dialogueSystemTrigger)
	{
		_dialogueSystemTrigger = dialogueSystemTrigger;
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
		var falfDuration = duration / 2;
		yield return StartCoroutine(FadeRoutine(0f, 1f, falfDuration));
		


		yield return new WaitForSeconds(falfDuration / 2);

		yield return StartCoroutine(FadeRoutine(1f, 0f, falfDuration));


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
        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0f, duration));
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
        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, duration));
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
        _fadeCoroutine = null;

		if (_dialogueSystemTrigger != null)
		{
			Debug.Log("OnUse");
			_dialogueSystemTrigger.OnUse();
			_dialogueSystemTrigger = null;
		}
		
    }
}