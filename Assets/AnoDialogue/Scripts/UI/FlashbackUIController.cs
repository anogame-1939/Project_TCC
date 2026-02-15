using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

namespace AnoGame.AnoDialogue.UI
{
    public class FlashbackUIController : DialogueUIBase
    {
        [Header("UI Components")]
        [SerializeField] private CanvasGroup ConversationPanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button continueButton;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.08f;

        [Header("Input")]
        [SerializeField] private Key advanceKey = Key.Space;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private bool isSkipping = false;
        private float inputCooldown = 0f;

        public override bool IsDialogueActive => ConversationPanel != null && ConversationPanel.blocksRaycasts;

        private void Awake()
        {
            SetPanelActive(false);
            if (backgroundImage) backgroundImage.enabled = false;
        }

        private void SetPanelActive(bool isActive)
        {
            if (ConversationPanel != null)
            {
                ConversationPanel.alpha = isActive ? 1f : 0f;
                ConversationPanel.interactable = isActive;
                ConversationPanel.blocksRaycasts = isActive;
            }
        }

        /// <summary>
        /// 背景画像を設定する。Timeline等から呼ばれる。
        /// </summary>
        public void SetBackgroundImage(Sprite sprite)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = sprite;
            }
        }

        /// <summary>
        /// 背景画像をクリアする。
        /// </summary>
        public void ClearBackgroundImage()
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.enabled = false;
            }
        }

        public override void ShowConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            SetPanelActive(true);
            inputCooldown = 0.2f;

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;

            // 背景画像を表示
            if (backgroundImage && backgroundImage.sprite != null)
            {
                backgroundImage.enabled = true;
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));
        }

        public override void PreviewConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            SetPanelActive(true);

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;

            if (bodyText)
            {
                bodyText.text = unit.BodyText;
                bodyText.maxVisibleCharacters = 99999;
            }

            if (backgroundImage && backgroundImage.sprite != null)
            {
                backgroundImage.enabled = true;
            }
        }

        public override void Close()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            SetPanelActive(false);

            // 背景画像を非表示
            if (backgroundImage)
            {
                backgroundImage.enabled = false;
            }
        }

        private IEnumerator TypeText(string content)
        {
            isTyping = true;
            isSkipping = false;

            if (bodyText)
            {
                bodyText.text = content;
                bodyText.maxVisibleCharacters = 0;
                bodyText.ForceMeshUpdate();

                int total = bodyText.textInfo.characterCount;

                if (continueButton) continueButton.gameObject.SetActive(false);

                WaitForSeconds wait = new WaitForSeconds(typingSpeed);

                for (int i = 1; i <= total; i++)
                {
                    bodyText.maxVisibleCharacters = i;
                    if (isSkipping) yield return null;
                    else yield return wait;
                }
                bodyText.maxVisibleCharacters = total;
            }

            isTyping = false;
            isSkipping = false;

            if (continueButton) continueButton.gameObject.SetActive(true);
        }

        public override void OnClickNext()
        {
            if (isTyping)
            {
                isSkipping = true;
                return;
            }

            if (!string.IsNullOrEmpty(currentUnit.NextID))
            {
                DialogueManager.Instance.StartConversation(currentUnit.NextID, this.StyleName);
            }
            else
            {
                Close();
            }
        }

        private void Update()
        {
            if (inputCooldown > 0f) inputCooldown -= Time.deltaTime;

            if (IsDialogueActive && inputCooldown <= 0f)
            {
                if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
                {
                    OnClickNext();
                }
            }
        }
    }
}
