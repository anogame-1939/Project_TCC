using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

namespace AnoGame.AnoDialogue.UI
{
    public class NarrationUIController : DialogueUIBase
    {
        [Header("UI Components")]
        [SerializeField] private CanvasGroup ConversationPanel;
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private Button continueButton;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
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

        public override void ShowConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            SetPanelActive(true);
            inputCooldown = 0.2f;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));
        }

        public override void PreviewConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            SetPanelActive(true);
            if (narrationText) narrationText.text = unit.BodyText;
        }

        public override void Close()
        {
            SetPanelActive(false);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        }

        private IEnumerator TypeText(string content)
        {
            isTyping = true;
            isSkipping = false;

            if (narrationText)
            {
                narrationText.text = content;
                narrationText.maxVisibleCharacters = 0;
                narrationText.ForceMeshUpdate();

                int total = narrationText.textInfo.characterCount;
                WaitForSeconds wait = new WaitForSeconds(typingSpeed);

                for (int i = 1; i <= total; i++)
                {
                    narrationText.maxVisibleCharacters = i;
                    if (isSkipping) yield return null;
                    else yield return wait;
                }
                narrationText.maxVisibleCharacters = total;
            }

            isTyping = false;
            isSkipping = false;
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
