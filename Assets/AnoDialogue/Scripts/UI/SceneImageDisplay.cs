using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace AnoGame.AnoDialogue.UI
{
    /// <summary>
    /// 独立したシーン画像表示コンポーネント。
    /// DialogueUIController / FlashbackUIController とは独立した階層に配置し、
    /// CanvasGroup の競合を回避する。
    /// Timeline の SceneImageTrack から制御される。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SceneImageDisplay : MonoBehaviour
    {
        [Header("Style")]
        [Tooltip("対応するダイアログスタイル名（例: '通常', 'Flashback'）")]
        [SerializeField] private string styleName = "";

        [Header("Image")]
        [SerializeField] private Image targetImage;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.2f;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;

        public string StyleName => styleName;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            if (targetImage != null)
            {
                targetImage.sprite = null;
            }
        }

        private void Start()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterSceneImageDisplay(this, styleName);
                Debug.Log($"[SceneImageDisplay] Registered: styleName='{styleName}', go={gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[SceneImageDisplay] Start: DialogueManager.Instance is NULL! Cannot register styleName='{styleName}'");
            }
        }

        /// <summary>
        /// シーン画像を表示する。フェードインで表示される。
        /// </summary>
        public void Show(Sprite sprite)
        {
            Debug.Log($"[SceneImageDisplay] Show: sprite={(sprite != null ? sprite.name : "NULL")}, targetImage={(targetImage != null ? "OK" : "NULL")}, go={gameObject.name}");

            if (targetImage != null)
            {
                targetImage.sprite = sprite;
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(1f));
        }

        /// <summary>
        /// シーン画像を非表示にする。フェードアウトで非表示にされる。
        /// </summary>
        public void Hide()
        {
            Debug.Log($"[SceneImageDisplay] Hide: go={gameObject.name}");
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(0f));
        }

        private IEnumerator Fade(float targetAlpha)
        {
            if (_canvasGroup == null) yield break;

            float startAlpha = _canvasGroup.alpha;
            if (Mathf.Approximately(startAlpha, targetAlpha)) yield break;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = targetAlpha;

            // フェードアウト完了後にスプライトをクリア
            if (targetAlpha <= 0f && targetImage != null)
            {
                targetImage.sprite = null;
            }
        }
    }
}
