using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.AnoNarrative
{
    public class DialogueManager : MonoBehaviour
    {
        private static DialogueManager _instance;
        public static DialogueManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DialogueManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DialogueManager");
                        _instance = go.AddComponent<DialogueManager>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private MasterDialogueData masterData;

        // Runtime cache for O(1) lookup
        private Dictionary<string, ConversationUnit> conversationCache;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCache();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeCache()
        {
            conversationCache = new Dictionary<string, ConversationUnit>();
            if (masterData != null)
            {
                foreach (var unit in masterData.Conversations)
                {
                    if (!string.IsNullOrEmpty(unit.ID) && !conversationCache.ContainsKey(unit.ID))
                    {
                        conversationCache.Add(unit.ID, unit);
                    }
                    else if (conversationCache.ContainsKey(unit.ID))
                    {
                        UnityEngine.Debug.LogWarning($"[DialogueManager] Duplicate ID found: {unit.ID}");
                    }
                }
            }
        }

        public ConversationUnit GetConversation(string id)
        {
            if (conversationCache == null) InitializeCache();

            if (conversationCache.TryGetValue(id, out var unit))
            {
                return unit;
            }

            UnityEngine.Debug.LogError($"[DialogueManager] Conversation ID not found: {id}");
            return null;
        }

        // UI Reference
        private UI.DialogueUIController activeUI;

        public void RegisterUI(UI.DialogueUIController ui)
        {
            activeUI = ui;
        }

        public bool IsConversationActive => activeUI != null && activeUI.IsDialogueActive;

        public void StopConversation()
        {
            if (activeUI != null)
            {
                activeUI.Close();
            }
        }

        public void StartConversation(string id)
        {
            var unit = GetConversation(id);
            if (unit != null)
            {
                if (activeUI != null)
                {
                    activeUI.ShowConversation(unit);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[DialogueManager] No UI registered. Conversation content: {unit.BodyText}");
                }
            }
        }

        public void PreviewConversation(string id)
        {
            Debug.Log($"[DialogueManager] PreviewConversation requested for ID: {id}");

            // In Editor, activeUI might not be registered yet if Start() wasn't called.
            if (activeUI == null)
            {
#if UNITY_EDITOR
                activeUI = FindObjectOfType<UI.DialogueUIController>();
                if (activeUI != null) Debug.Log("[DialogueManager] Found ActiveUI via FindObjectOfType.");
#endif
            }

            var unit = GetConversation(id);
            if (unit != null)
            {
                if (activeUI != null)
                {
                    activeUI.PreviewConversation(unit);
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] ActiveUI is null.");
                }
            }
            else
            {
                Debug.LogWarning($"[DialogueManager] ConversationUnit not found for ID: {id}");
            }
        }

        public void AdvanceConversation()
        {
            if (activeUI != null)
            {
                activeUI.OnClickNext();
            }
        }

        public void StartSection(int episodeID, int chapterID, int sectionID)
        {
            if (masterData == null) return;

            // Find first unit that matches context.
            // -1 acts as a wildcard (similar to string.IsNullOrEmpty before)
            foreach (var unit in masterData.Conversations)
            {
                bool matchEp = episodeID == -1 || unit.EpisodeID == episodeID;
                bool matchCh = chapterID == -1 || unit.ChapterID == chapterID;
                bool matchSec = sectionID == -1 || unit.SectionID == sectionID;

                if (matchEp && matchCh && matchSec)
                {
                    StartConversation(unit.ID);
                    return;
                }
            }

            UnityEngine.Debug.LogWarning($"[DialogueManager] No conversation found for Ep:{episodeID} Ch:{chapterID} Sec:{sectionID}");
        }
        public IEnumerable<string> GetAllConversationIDs()
        {
            if (masterData != null)
            {
                foreach (var unit in masterData.Conversations)
                {
                    if (!string.IsNullOrEmpty(unit.ID)) yield return unit.ID;
                }
            }
        }
    }
}
