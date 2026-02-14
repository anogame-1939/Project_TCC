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
        [SerializeField] private GameObject panelObject;
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private Button continueButton;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private Key advanceKey = Key.Space;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private bool isSkipping = false;

        public override bool IsDialogueActive => panelObject != null && panelObject.activeSelf;

        private void Awake()
        {
            if (panelObject) panelObject.SetActive(false);
        }

        public override void ShowConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            if (panelObject) panelObject.SetActive(true);

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));
        }

        public override void PreviewConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            if (panelObject) panelObject.SetActive(true);
            if (narrationText) narrationText.text = unit.BodyText;
        }

        public override void Close()
        {
            if (panelObject) panelObject.SetActive(false);
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
