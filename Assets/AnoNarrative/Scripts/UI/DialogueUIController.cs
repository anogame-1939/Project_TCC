using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using AnoGame.AnoNarrative;

namespace AnoGame.AnoNarrative.UI
{
    public class DialogueUIController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject itemsParent; // The main panel to show/hide
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button continueButton;

        [Header("Choices")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;

        [Header("Input")]
        [SerializeField] private Key advanceKey = Key.Space;

        [Header("Auto Advance")]
        [SerializeField] private bool _isAutoAdvance = false;
        [SerializeField] private float autoAdvanceDelay = 1.0f;

        private ConversationUnit currentUnit;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private bool isSkipping = false;
        private float inputCooldown = 0f;

        public bool IsDialogueActive => itemsParent != null && itemsParent.activeSelf;

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

        private void Awake()
        {
            if (itemsParent) itemsParent.SetActive(false);
            if (choiceButtonPrefab) choiceButtonPrefab.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // Try register if Manager exists
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterUI(this);
            }
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
            inputCooldown = 0.2f; // Prevent immediate skip input

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(unit.BodyText));

            SetupChoices(unit);
        }

        public void PreviewConversation(ConversationUnit unit)
        {
            currentUnit = unit;
            if (itemsParent) itemsParent.SetActive(true);

            if (speakerNameText) speakerNameText.text = unit.SpeakerName;
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
            if (inputCooldown > 0f) inputCooldown -= Time.deltaTime;

            // Only listen if dialogue is active and cooldown passed
            if (itemsParent != null && itemsParent.activeSelf && inputCooldown <= 0f)
            {
                if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame)
                {
                    OnClickNext();
                }
            }
        }
    }
}
