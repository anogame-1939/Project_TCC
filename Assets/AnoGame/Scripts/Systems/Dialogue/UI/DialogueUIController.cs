using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

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

        [Header("Input")]
        [SerializeField] private Key advanceKey = Key.Space;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private bool isSkipping = false;

        public bool IsDialogueActive => itemsParent != null && itemsParent.activeSelf;

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

        [Header("Auto Advance")]
        [SerializeField] private bool _isAutoAdvance = false;
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
        [SerializeField] private float autoAdvanceDelay = 1.0f;
        [SerializeField] private Button continueButton;

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

            for (int i = 0; i <= totalVisibleCharacters; i++)
            {
                bodyText.maxVisibleCharacters = i;

                if (isSkipping)
                {
                    yield return null;
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }

            bodyText.maxVisibleCharacters = totalVisibleCharacters;
            isTyping = false;
            isSkipping = false;

            if (IsAutoAdvance)
            {
                if (currentUnit.Choices == null || currentUnit.Choices.Count == 0)
                {
                    yield return new WaitForSeconds(autoAdvanceDelay);
                    OnClickNext();
                }
            }
            else
            {
                if (currentUnit.Choices == null || currentUnit.Choices.Count == 0)
                {
                    if (continueButton) continueButton.gameObject.SetActive(true);
                }
            }
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

        private void Update()
        {
            // Only listen if dialogue is active
            if (itemsParent != null && itemsParent.activeSelf)
            {
                if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
                {
                    OnClickNext();
                }
            }
        }
    }
}
