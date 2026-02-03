using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

namespace AnoGame.AnoNarrative.UI
{
    public class FlashbackUIController : DialogueUIBase
    {
        [Header("UI Components")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private TextMeshProUGUI centerText;
        [SerializeField] private Image filterImage; // Sepia or B&W overlay

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.08f; // Slower for flashback
        [SerializeField] private Key advanceKey = Key.Space;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private bool isSkipping = false;

        public override bool IsDialogueActive => overlayPanel != null && overlayPanel.activeSelf;

        private void Awake()
        {
            if (overlayPanel) overlayPanel.SetActive(false);
        }

        public override void ShowConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            if (overlayPanel) overlayPanel.SetActive(true);

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));
        }

        public override void PreviewConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            if (overlayPanel) overlayPanel.SetActive(true);
            if (centerText) centerText.text = unit.BodyText;
        }

        public override void Close()
        {
            if (overlayPanel) overlayPanel.SetActive(false);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        }

        private IEnumerator TypeText(string content)
        {
            isTyping = true;
            isSkipping = false;

            if (centerText)
            {
                centerText.text = content;
                centerText.maxVisibleCharacters = 0;
                centerText.ForceMeshUpdate();

                int total = centerText.textInfo.characterCount;
                WaitForSeconds wait = new WaitForSeconds(typingSpeed);

                for (int i = 1; i <= total; i++)
                {
                    centerText.maxVisibleCharacters = i;
                    if (isSkipping) yield return null;
                    else yield return wait;
                }
                centerText.maxVisibleCharacters = total;
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
                DialogueManager.Instance.StartConversation(currentUnit.NextID, this.Style);
            }
            else
            {
                Close();
            }
        }

        private void Update()
        {
            if (IsDialogueActive)
            {
                if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
                {
                    OnClickNext();
                }
            }
        }
    }
}
