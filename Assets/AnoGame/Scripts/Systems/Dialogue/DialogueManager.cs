using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Systems.Dialogue
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
                        Debug.LogWarning($"[DialogueManager] Duplicate ID found: {unit.ID}");
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

            Debug.LogError($"[DialogueManager] Conversation ID not found: {id}");
            return null;
        }

        // UI Reference
        private UI.DialogueUIController activeUI;

        public void RegisterUI(UI.DialogueUIController ui)
        {
            activeUI = ui;
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
                    Debug.LogWarning($"[DialogueManager] No UI registered. Conversation content: {unit.BodyText}");
                }
            }
        }
    }
}
