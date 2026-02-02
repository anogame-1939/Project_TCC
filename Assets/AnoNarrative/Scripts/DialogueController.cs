using UnityEngine;
using AnoGame.AnoNarrative.UI;

namespace AnoGame.AnoNarrative
{
    /// <summary>
    /// Controller to trigger specific dialogues based on filtered criteria (Episode/Chapter/Section).
    /// Supersedes the legacy DialogueTrigger and DebugController for production use.
    /// </summary>
    public class DialogueController : MonoBehaviour
    {
        [Header("Filter Settings")]
        [Tooltip("Episode ID to filter conversations (Default: -1)")]
        public int TargetEpisode = -1;

        [Tooltip("Chapter ID to filter conversations (Default: -1)")]
        public int TargetChapter = -1;

        [Tooltip("Section ID to filter conversations (Default: -1)")]
        public int TargetSection = -1;

        [Header("Playback Settings")]
        [Tooltip("The specific Conversation ID to play. Use the Inspector buttons to select from filtered results.")]
        public string TargetID;

        [Tooltip("If true, the conversation will start automatically when this component starts.")]
        public bool PlayOnStart = false;

        [Tooltip("If true, the dialogue UI will be set to Auto-Advance mode.")]
        public bool AutoAdvance = false;

        private void Start()
        {
            if (PlayOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            if (string.IsNullOrEmpty(TargetID))
            {
                Debug.LogWarning($"[DialogueController] TargetID is empty on {gameObject.name}. Cannot play dialogue.", gameObject);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueController] DialogueManager Instance is null. Cannot play dialogue.");
                return;
            }

            ApplySettings();
            DialogueManager.Instance.StartConversation(TargetID);
        }

        /// <summary>
        /// Plays a specific conversation by ID, overriding the inspector TargetID.
        /// </summary>
        /// <param name="conversationID">The ID of the conversation to play.</param>
        public void Play(string conversationID)
        {
            if (string.IsNullOrEmpty(conversationID))
            {
                Debug.LogWarning($"[DialogueController] conversationID is empty on {gameObject.name}. Cannot play dialogue.", gameObject);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueController] DialogueManager Instance is null. Cannot play dialogue.");
                return;
            }

            ApplySettings();
            DialogueManager.Instance.StartConversation(conversationID);
        }

        private void ApplySettings()
        {
            // Apply AutoAdvance setting if UI controller is available
            var ui = FindFirstObjectByType<DialogueUIController>();
            if (ui != null)
            {
                ui.SetAutoAdvance(AutoAdvance);
            }
        }
    }
}
