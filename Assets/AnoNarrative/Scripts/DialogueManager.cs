using System.Collections.Generic;
using UnityEngine;
using AnoGame.AnoNarrative.Data;

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
                    _instance = FindFirstObjectByType<DialogueManager>();
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
        [SerializeField] private DialogueActorDatabase actorDatabase;

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

        public Sprite GetActorSprite(string name)
        {
            if (actorDatabase != null)
            {
                return actorDatabase.GetPortrait(name);
            }
            return null;
        }

        // UI Reference
        private Dictionary<DialogueStyle, UI.DialogueUIBase> registeredUIs = new Dictionary<DialogueStyle, UI.DialogueUIBase>();
        private UI.DialogueUIBase activeUI;

        public void RegisterUI(UI.DialogueUIBase ui, DialogueStyle style)
        {
            if (style != null)
            {
                if (!registeredUIs.ContainsKey(style))
                {
                    registeredUIs.Add(style, ui);
                }
                else
                {
                    registeredUIs[style] = ui;
                }
            }
            else
            {
                // Fallback for default/null style, maybe key null?
                // Or handle generic "Standard" if style is missing.
                // Let's treat null as a specific "Default" slot for now, or just warn.
                Debug.LogWarning("[DialogueManager] Registering UI with null style. This will be the default fallback.");
                if (!registeredUIs.ContainsKey(null))
                    registeredUIs.Add(null, ui);
                else
                    registeredUIs[null] = ui;
            }
        }

        public bool IsConversationActive => activeUI != null && activeUI.IsDialogueActive;

        public void StopConversation()
        {
            if (activeUI != null)
            {
                activeUI.Close();
                activeUI = null;
            }
        }

        public void StartConversation(string id, DialogueStyle style = null)
        {
            var unit = GetConversation(id);
            if (unit != null)
            {
                // Select UI
                UI.DialogueUIBase targetUI = ResolveUI(style);

                if (targetUI != null)
                {
                    // If switching UIs, close the old one?
                    if (activeUI != null && activeUI != targetUI)
                    {
                        activeUI.Close();
                    }

                    activeUI = targetUI;
                    activeUI.ShowConversation(unit);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[DialogueManager] No UI registered for style: {(style != null ? style.name : "Default")}. Content: {unit.BodyText}");
                }
            }
        }

        private UI.DialogueUIBase ResolveUI(DialogueStyle style)
        {
            // 1. Try specific style
            if (style != null && registeredUIs.TryGetValue(style, out var ui))
            {
                return ui;
            }

            // 2. Try null/Default style
            if (registeredUIs.TryGetValue(null, out var defaultUI))
            {
                return defaultUI;
            }

            // 3. Fallback to any first registered UI
            if (registeredUIs.Count > 0)
            {
                // Just grab the first one
                var e = registeredUIs.GetEnumerator();
                e.MoveNext();
                return e.Current.Value;
            }

            return null;
        }

        public void PreviewConversation(string id, DialogueStyle style = null)
        {
            Debug.Log($"[DialogueManager] PreviewConversation requested for ID: {id}");

            // In Editor, activeUI might not be registered if Start() wasn't called.
            if (registeredUIs.Count == 0)
            {
#if UNITY_EDITOR
                // Try finding any in scene
                var uis = FindObjectsByType<UI.DialogueUIBase>(FindObjectsSortMode.None);
                foreach (var u in uis)
                {
                    RegisterUI(u, u.Style);
                }
#endif
            }

            var unit = GetConversation(id);
            if (unit != null)
            {
                var targetUI = ResolveUI(style);
                if (targetUI != null)
                {
                    if (activeUI != null && activeUI != targetUI) activeUI.Close();
                    activeUI = targetUI;
                    activeUI.PreviewConversation(unit);
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] No UI found for preview.");
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
                    StartConversation(unit.ID, null); // Default style for now
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
