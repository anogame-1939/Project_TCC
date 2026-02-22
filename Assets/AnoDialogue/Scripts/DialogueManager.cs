using System.Collections.Generic;
using UnityEngine;
using AnoGame.AnoDialogue.Data;

namespace AnoGame.AnoDialogue
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

        // ── テンプレート変数 ──
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCache();
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
                UnityEngine.Debug.Log($"[DialogueManager] Cache initialized: {conversationCache.Count} entries from {masterData.name}");
            }
            else
            {
                UnityEngine.Debug.LogError("[DialogueManager] masterData is NULL! Cache will be empty.");
            }
        }

        // ── テンプレート変数 API ──

        /// <summary>
        /// テンプレート変数を設定する。
        /// BodyText 内の {key} が value に置換される。
        /// </summary>
        public void SetVariable(string key, string value)
        {
            _variables[key] = value;
        }

        /// <summary>
        /// 全テンプレート変数をクリアする。
        /// </summary>
        public void ClearVariables()
        {
            _variables.Clear();
        }

        /// <summary>
        /// テキスト内の {key} をセット済みの変数値で置換する。
        /// </summary>
        public string ResolveVariables(string text)
        {
            if (string.IsNullOrEmpty(text) || _variables.Count == 0)
                return text;

            foreach (var kv in _variables)
            {
                text = text.Replace($"{{{kv.Key}}}", kv.Value);
            }
            return text;
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
        private Dictionary<string, UI.DialogueUIBase> registeredUIs = new Dictionary<string, UI.DialogueUIBase>();
        private UI.DialogueUIBase activeUI;

        // SceneImageDisplay Reference
        private Dictionary<string, UI.SceneImageDisplay> registeredSceneImageDisplays = new Dictionary<string, UI.SceneImageDisplay>();

        public void RegisterSceneImageDisplay(UI.SceneImageDisplay display, string styleName)
        {
            if (string.IsNullOrEmpty(styleName)) return;

            if (!registeredSceneImageDisplays.ContainsKey(styleName))
            {
                registeredSceneImageDisplays.Add(styleName, display);
            }
            else
            {
                registeredSceneImageDisplays[styleName] = display;
            }
        }

        /// <summary>
        /// スタイル名に対応する SceneImageDisplay を取得する。
        /// </summary>
        public UI.SceneImageDisplay GetSceneImageDisplay(string styleName)
        {
            if (string.IsNullOrEmpty(styleName)) return null;
            registeredSceneImageDisplays.TryGetValue(styleName, out var display);
            return display;
        }

        public void RegisterUI(UI.DialogueUIBase ui, string styleName)
        {
            if (string.IsNullOrEmpty(styleName)) return;

            if (!registeredUIs.ContainsKey(styleName))
            {
                registeredUIs.Add(styleName, ui);
            }
            else
            {
                registeredUIs[styleName] = ui;
            }

            // 最初に登録されたUIをデフォルトのactiveUIとする
            if (activeUI == null)
            {
                activeUI = ui;
            }
        }

        /// <summary>
        /// スタイル名に対応する登録済みUIを取得する。
        /// </summary>
        public UI.DialogueUIBase GetUI(string styleName)
        {
            if (string.IsNullOrEmpty(styleName)) return null;
            registeredUIs.TryGetValue(styleName, out var ui);
            return ui;
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

        public void StartConversation(string id, string styleName = null)
        {
            var unit = GetConversation(id);
            if (unit != null)
            {
                // Select UI
                UI.DialogueUIBase targetUI = ResolveUI(styleName);

                if (targetUI != null)
                {
                    // If switching UIs, close the old one?
                    if (activeUI != null && activeUI != targetUI)
                    {
                        activeUI.Close();
                    }

                    activeUI = targetUI;

                    // テンプレート変数を適用（元データを汚さないようコピーして置換）
                    var resolved = ResolveUnit(unit);
                    activeUI.ShowConversation(resolved);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[DialogueManager] No UI registered for style: {(styleName ?? "Default")}. Content: {unit.BodyText}");
                }
            }
        }

        /// <summary>
        /// ConversationUnit の BodyText にテンプレート変数を適用したコピーを返す。
        /// 変数が未設定、またはプレースホルダーが無い場合は元のインスタンスをそのまま返す。
        /// </summary>
        private ConversationUnit ResolveUnit(ConversationUnit unit)
        {
            if (_variables.Count == 0 || unit.BodyText == null || !unit.BodyText.Contains("{"))
                return unit;

            var resolved = new ConversationUnit
            {
                ID = unit.ID,
                EpisodeID = unit.EpisodeID,
                ChapterID = unit.ChapterID,
                SectionID = unit.SectionID,
                NodeNumber = unit.NodeNumber,
                SectionName = unit.SectionName,
                SpeakerName = unit.SpeakerName,
                BodyText = ResolveVariables(unit.BodyText),
                NextID = unit.NextID,
                Choices = unit.Choices,
                Position = unit.Position,
            };
            return resolved;
        }

        private UI.DialogueUIBase ResolveUI(string styleName)
        {
            // スタイル名が指定されている場合、辞書から検索
            if (!string.IsNullOrEmpty(styleName))
            {
                if (registeredUIs.TryGetValue(styleName, out var ui))
                {
                    return ui;
                }

                UnityEngine.Debug.LogWarning($"[DialogueManager] Style '{styleName}' not found in registered UIs.");
            }

            // 未指定またはスタイル未登録の場合、activeUI を返す
            return activeUI;
        }

        public void PreviewConversation(string id, string styleName = null)
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
                    RegisterUI(u, u.StyleName);
                }
#endif
            }

            var unit = GetConversation(id);
            if (unit != null)
            {
                var targetUI = ResolveUI(styleName);
                if (targetUI != null)
                {
                    if (activeUI != null && activeUI != targetUI) activeUI.Close();
                    activeUI = targetUI;

                    // テンプレート変数を適用
                    var resolved = ResolveUnit(unit);
                    activeUI.PreviewConversation(resolved);
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
