using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Assuming TextMeshPro is used, fallback to Text if not

namespace AnoGame.Systems.Dialogue.UI
{
    public class DialogueUIController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject itemsParent; // The main panel to show/hide
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI bodyText;

        [Header("Choices")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private bool isTyping = false;

        private void Awake()
        {
            if (itemsParent) itemsParent.SetActive(false);
            if (choiceButtonPrefab) choiceButtonPrefab.gameObject.SetActive(false);
        }

        private void Start()
        {
            // Register self to Manager (Simple singleton pattern or dependency injection)
            // For now, we assume Manager calls us or we set it up in Inspector
            DialogueManager.Instance.RegisterUI(this);
        }

        public void ShowConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            if (itemsParent) itemsParent.SetActive(true);

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));

            SetupChoices(unit);
        }

        private IEnumerator TypeText(string content)
        {
            isTyping = true;
            bodyText.text = "";
            foreach (char c in content)
            {
                bodyText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            isTyping = false;
        }

        private void SetupChoices(ConversationUnit unit)
        {
            // Clear old choices
            foreach (Transform child in choiceContainer)
            {
                if (child.gameObject != choiceButtonPrefab.gameObject)
                    Destroy(child.gameObject);
            }

            if (unit.Choices != null && unit.Choices.Count > 0)
            {
                foreach (var choice in unit.Choices)
                {
                    var btn = Instantiate(choiceButtonPrefab, choiceContainer);
                    btn.gameObject.SetActive(true);
                    var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (text) text.text = choice.ChoiceText;

                    btn.onClick.AddListener(() => OnChoiceSelected(choice));
                }
            }
        }

        public void OnClickNext()
        {
            if (isTyping)
            {
                // Skip typing
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                bodyText.text = currentUnit.BodyText;
                isTyping = false;
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
                DialogueManager.Instance.StartConversation(currentUnit.NextID);
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
                DialogueManager.Instance.StartConversation(choice.TargetID);
            }
            else
            {
                Close();
            }
        }

        public void Close()
        {
            if (itemsParent) itemsParent.SetActive(false);
            // Notify Manager?
        }
    }
}
