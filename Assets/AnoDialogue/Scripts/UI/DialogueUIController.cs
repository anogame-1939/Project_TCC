using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AnoGame.AnoDialogue.UI
{
    public class DialogueUIController : DialogueUIBase
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Button continueButton;

        [Header("Scene Image")]
        [SerializeField] private Image locationImage;
        [Tooltip("マスク含む親のCanvasGroup。フェードの制御に使用")]
        [SerializeField] private CanvasGroup sceneImageCanvasGroup;

        [Header("Choices")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private float portraitFadeDuration = 0.2f;

        [Header("Input")]
        [SerializeField] private Key advanceKey = Key.Space;

        [Header("Auto Advance")]
        [SerializeField] private bool _isAutoAdvance = false;
        [SerializeField] private float autoAdvanceDelay = 1.0f;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private Coroutine currentPortraitRoutine;
        private Coroutine currentSceneImageRoutine;
        private Coroutine activeChoiceRoutine;
        private bool isTyping = false;
        private bool isSkipping = false;
        private float inputCooldown = 0f;


        public bool IsAutoAdvance
        {
            get => _isAutoAdvance;
            private set
            {
                if (_isAutoAdvance != value)
                {
                    _isAutoAdvance = value;
                    OnAutoAdvanceChanged?.Invoke(_isAutoAdvance);
                }
            }
        }

        public System.Action<bool> OnAutoAdvanceChanged;

        public void SetAutoAdvance(bool isActive)
        {
            IsAutoAdvance = isActive;
        }

        public void ToggleAutoAdvance()
        {
            IsAutoAdvance = !IsAutoAdvance;
        }

        protected override void Awake()
        {
            base.Awake();
            if (choiceButtonPrefab) choiceButtonPrefab.gameObject.SetActive(false);
            if (portraitImage) portraitImage.enabled = false;
            if (sceneImageCanvasGroup)
            {
                sceneImageCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// シーン画像を設定する（通常スタイルではロケーション画像として使用）。
        /// </summary>
        public override void SetSceneImage(Sprite sprite)
        {
            if (locationImage != null)
            {
                locationImage.sprite = sprite;
            }
        }

        /// <summary>
        /// シーン画像をクリアする。フェードアウトして非表示にする。
        /// </summary>
        public override void ClearSceneImage()
        {
            if (locationImage != null)
            {
                if (currentSceneImageRoutine != null) StopCoroutine(currentSceneImageRoutine);
                currentSceneImageRoutine = StartCoroutine(FadeSceneImage(null));
            }
        }

        // Start/OnEnable handled by Base, but we can override if needed. 
        // Base Start/OnEnable registers UI.

        public override void ShowConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            SetPanelActive(true);
            inputCooldown = 0.2f; // Prevent immediate skip input

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;

            if (portraitImage)
            {
                var sprite = DialogueManager.Instance.GetActorSprite(unit.SpeakerName);
                UpdatePortrait(sprite);
            }

            // ロケーション画像のフェード表示
            if (locationImage && locationImage.sprite != null)
            {
                if (currentSceneImageRoutine != null) StopCoroutine(currentSceneImageRoutine);
                currentSceneImageRoutine = StartCoroutine(FadeSceneImage(locationImage.sprite));
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));

            SetupChoices(unit);
        }

        public override void PreviewConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            SetPanelActive(true);

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;

            if (portraitImage)
            {
                var sprite = DialogueManager.Instance.GetActorSprite(unit.SpeakerName);
                UpdatePortrait(sprite);
            }

            if (bodyText)
            {
                bodyText.text = unit.BodyText;
                bodyText.maxVisibleCharacters = 99999;
            }

            // Hide choices in preview for now or show them static?
            SetupChoices(unit);
        }

        private IEnumerator TypeText(string content)
        {
            isTyping = true;
            isSkipping = false;
            bodyText.text = content;
            bodyText.maxVisibleCharacters = 0;

            // Force update to calculate correct character count (ignoring rich text tags)
            bodyText.ForceMeshUpdate();
            int totalVisibleCharacters = bodyText.textInfo.characterCount;

            if (continueButton) continueButton.gameObject.SetActive(false);

            WaitForSeconds wait = new WaitForSeconds(typingSpeed);

            for (int i = 1; i <= totalVisibleCharacters; i++)
            {
                bodyText.maxVisibleCharacters = i;

                if (isSkipping)
                {
                    yield return null;
                }
                else
                {
                    yield return wait;
                }
            }

            bodyText.maxVisibleCharacters = totalVisibleCharacters;
            isTyping = false;
            isSkipping = false;

            if (IsAutoAdvance)
            {
                // Wait delay then next
                // But if choices exist, we must wait for user? Yes.
                if (currentUnit.Choices == null || currentUnit.Choices.Count == 0)
                {
                    yield return new WaitForSeconds(autoAdvanceDelay);
                    OnClickNext();
                }
                else
                {
                    // Choices will be shown, user must pick.
                }
            }
            else
            {
                // Show Continue Button if no choices (or even if choices? usually choices hide continue)
                if (currentUnit.Choices == null || currentUnit.Choices.Count == 0)
                {
                    if (continueButton) continueButton.gameObject.SetActive(true);
                }
            }
        }

        private void SetupChoices(ConversationUnit unit)
        {
            if (activeChoiceRoutine != null) StopCoroutine(activeChoiceRoutine);
            activeChoiceRoutine = StartCoroutine(SetupChoicesRoutine(unit));
        }

        private IEnumerator SetupChoicesRoutine(ConversationUnit unit)
        {
            // Clear old choices immediately
            foreach (Transform child in choiceContainer)
            {
                if (child.gameObject != choiceButtonPrefab.gameObject)
                    Destroy(child.gameObject);
            }

            if (unit.Choices != null && unit.Choices.Count > 0)
            {
                // Wait 0.5s before showing choices
                yield return new WaitForSeconds(0.5f);

                GameObject firstButton = null;
                foreach (var choice in unit.Choices)
                {
                    var btn = Instantiate(choiceButtonPrefab, choiceContainer);
                    btn.gameObject.SetActive(true);
                    if (firstButton == null) firstButton = btn.gameObject;

                    var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (text) text.text = choice.ChoiceText;

                    btn.onClick.AddListener(() => OnChoiceSelected(choice));
                }

                if (firstButton != null)
                {
                    StartCoroutine(SelectFirstChoiceDelayed(firstButton));
                }
            }
        }

        public override void OnClickNext()
        {
            if (isTyping)
            {
                // Skip typing
                isSkipping = true;
                return;
            }

            // Logic to go to next
            if (currentUnit.Choices != null && currentUnit.Choices.Count > 0)
            {
                // Waiting for choice, do nothing or flash choice area
                return;
            }

            if (!string.IsNullOrEmpty(currentUnit.NextID))
            {
                // NOTE: This always uses default style for next? 
                // Or should it use the SAME style as current?
                // DialogueManager.Instance.StartConversation doesn't know context unless we pass it.
                // For now, assume NextID flows in the same style or defined by that unit logic (not implemented yet).
                // Wait, if I call StartConversation(id), it uses default or resolved style.
                // Ideally check if unit has style data, but ConversationUnit doesn't have style.
                // So we should probably keep using the CURRENT style.
                // But `StartConversation` with checking `this.Style` might be better.
                DialogueManager.Instance.StartConversation(currentUnit.NextID, this.StyleName);
            }
            else
            {
                Close();
            }
        }

        private void OnChoiceSelected(Choice choice)
        {
            if (!string.IsNullOrEmpty(choice.TargetID))
            {
                DialogueManager.Instance.StartConversation(choice.TargetID, this.StyleName);
            }
            else
            {
                Close();
            }
        }

        public override void Close()
        {
            if (activeChoiceRoutine != null) StopCoroutine(activeChoiceRoutine);
            if (currentPortraitRoutine != null) StopCoroutine(currentPortraitRoutine);
            SetPanelActive(false);

            if (portraitImage)
            {
                portraitImage.enabled = false;
                Color c = portraitImage.color;
                c.a = 0f;
                portraitImage.color = c;
            }

            // ロケーション画像をフェードアウト
            if (sceneImageCanvasGroup && sceneImageCanvasGroup.alpha > 0f)
            {
                if (currentSceneImageRoutine != null) StopCoroutine(currentSceneImageRoutine);
                currentSceneImageRoutine = StartCoroutine(FadeSceneImage(null));
            }
        }

        private void Update()
        {
            if (inputCooldown > 0f) inputCooldown -= Time.deltaTime;

            // Only listen if dialogue is active and cooldown passed
            if (IsDialogueActive && inputCooldown <= 0f)
            {
                if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
                {
                    OnClickNext();
                }
            }
        }
        private void UpdatePortrait(Sprite sprite)
        {
            if (currentPortraitRoutine != null) StopCoroutine(currentPortraitRoutine);
            currentPortraitRoutine = StartCoroutine(ExecutePortraitFade(sprite));
        }

        private IEnumerator ExecutePortraitFade(Sprite newSprite)
        {
            // If we are showing a new sprite
            if (newSprite != null)
            {
                // If it was already active and showing something, maybe we just swap?
                // Or crossfade? For simplicity:
                // If invisible, fade in.
                // If visible, just swap sprite (instant) or quick fade out/in?
                // Let's go with: If active, swap immediately. If inactive, fade in.

                if (portraitImage.enabled && portraitImage.color.a > 0.9f)
                {
                    portraitImage.sprite = newSprite;
                    yield break;
                }

                portraitImage.sprite = newSprite;
                portraitImage.enabled = true;

                Color c = portraitImage.color;
                float startAlpha = c.a;
                float t = 0f;

                while (t < portraitFadeDuration)
                {
                    t += Time.deltaTime;
                    c.a = Mathf.Lerp(startAlpha, 1f, t / portraitFadeDuration);
                    portraitImage.color = c;
                    yield return null;
                }
                c.a = 1f;
                portraitImage.color = c;
            }
            else
            {
                // Fade out
                if (!portraitImage.enabled) yield break;

                Color c = portraitImage.color;
                float startAlpha = c.a;
                float t = 0f;

                while (t < portraitFadeDuration)
                {
                    t += Time.deltaTime;
                    c.a = Mathf.Lerp(startAlpha, 0f, t / portraitFadeDuration);
                    portraitImage.color = c;
                    yield return null;
                }
                c.a = 0f;
                portraitImage.color = c;
                portraitImage.enabled = false;
            }
        }

        /// <summary>
        /// ロケーション画像のフェードイン/フェードアウト。
        /// CanvasGroup.alphaで制御するため、マスク含む親要素全体がフェードする。
        /// sprite != null でフェードイン、null でフェードアウト。
        /// </summary>
        private IEnumerator FadeSceneImage(Sprite newSprite)
        {
            if (sceneImageCanvasGroup == null) yield break;

            if (newSprite != null)
            {
                // 既に同じスプライトで表示中ならスキップ
                if (locationImage != null && sceneImageCanvasGroup.alpha > 0.9f
                    && locationImage.sprite == newSprite)
                {
                    yield break;
                }

                if (locationImage != null)
                {
                    locationImage.sprite = newSprite;
                }

                float startAlpha = sceneImageCanvasGroup.alpha;
                float t = 0f;

                while (t < portraitFadeDuration)
                {
                    t += Time.deltaTime;
                    sceneImageCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t / portraitFadeDuration);
                    yield return null;
                }
                sceneImageCanvasGroup.alpha = 1f;
            }
            else
            {
                // フェードアウト
                if (sceneImageCanvasGroup.alpha <= 0f) yield break;

                float startAlpha = sceneImageCanvasGroup.alpha;
                float t = 0f;

                while (t < portraitFadeDuration)
                {
                    t += Time.deltaTime;
                    sceneImageCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / portraitFadeDuration);
                    yield return null;
                }
                sceneImageCanvasGroup.alpha = 0f;

                if (locationImage != null)
                {
                    locationImage.sprite = null;
                }
            }
        }

        private IEnumerator SelectFirstChoiceDelayed(GameObject target)
        {
            yield return new WaitForSeconds(1.0f);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
