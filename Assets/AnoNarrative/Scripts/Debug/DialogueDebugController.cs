using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 追加：InputSystemの名前空間
using UDebug = UnityEngine.Debug;

namespace AnoGame.Systems.Dialogue.Debug
{
    public class DialogueDebugController : MonoBehaviour
    {
        [Header("Runtime Debug")]
        [Tooltip("Available Conversation IDs (auto-filled if MasterData is accessible via Manager)")]
        public List<string> AvailableIDs = new List<string>();

        [Header("Section Playback")]
        public string TargetEpisode;
        public string TargetChapter;
        public string TargetSection;

        [Tooltip("Enable Auto-Advance for testing")]
        public bool AutoAdvance = false;

        [Header("Direct Playback")]
        [Tooltip("ID to play when 'Play' button is clicked")]
        public string TargetID;

        [Header("On-Screen Console")]
        public bool ShowOnScreen = false;
        [Tooltip("If true, pressing the ToggleKey will also advance the dialogue if it is active")]
        public bool AdvanceWithToggleKey = true;

        // 修正点1：KeyCode(旧) から Key(新) に変更
        // これによりInspectorで新しいInputSystemのキー一覧から選択できるようになります
        public Key ToggleKey = Key.Backquote; // "~" key

        private bool _isVisible = false;
        private Vector2 _scrollPos;

        private void Start()
        {
            RefreshIDList();
        }

        private void Update()
        {
            // 修正点2：Input.GetKeyDown(旧) から Keyboard.current[key].wasPressedThisFrame(新) に変更
            // Keyboard.currentがnullでないかチェックするのが安全です
            if (Keyboard.current != null && Keyboard.current[ToggleKey].wasPressedThisFrame)
            {
                UDebug.Log("ToggleKey pressed");

                // If dialogue is active and we want to advance with toggle key
                if (AdvanceWithToggleKey)
                {
                    var ui = FindFirstObjectByType<UI.DialogueUIController>();
                    if (ui != null && ui.IsDialogueActive)
                    {
                        ui.OnClickNext();
                    }
                }

                _isVisible = !_isVisible;
                if (_isVisible) RefreshIDList();
            }
        }

        public void PlayCurrentTarget()
        {
            ApplyDebugSettings();

            if (string.IsNullOrEmpty(TargetID))
            {
                UnityEngine.Debug.LogWarning("[DialogueDebug] No TargetID specified.");
                return;
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(TargetID);
            }
        }

        public void PlaySection()
        {
            ApplyDebugSettings();

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartSection(TargetEpisode, TargetChapter, TargetSection);
            }
        }

        private void ApplyDebugSettings()
        {
            if (DialogueManager.Instance != null)
            {
                // Find UI and apply settings
                var ui = FindFirstObjectByType<UI.DialogueUIController>();
                if (ui != null)
                {
                    ui.SetAutoAdvance(AutoAdvance);
                }
            }
        }

        [ContextMenu("Refresh IDs")]
        public void RefreshIDList()
        {
            if (DialogueManager.Instance != null)
            {
                var ids = DialogueManager.Instance.GetAllConversationIDs();
                if (ids != null)
                {
                    AvailableIDs = new List<string>(ids);
                }
            }
#if UNITY_EDITOR
            else
            {
                // Fallback for Editor-time execution of ContextMenu
                // Find DialogueManager in scene
                var mgr = FindFirstObjectByType<DialogueManager>();
                if (mgr != null)
                {
                    var ids = mgr.GetAllConversationIDs();
                    if (ids != null) AvailableIDs = new List<string>(ids);
                }
            }
#endif
        }

        private void OnGUI()
        {
            if (!ShowOnScreen || !_isVisible) return;

            float width = 300;
            float height = 400;
            Rect windowRect = new Rect(20, 20, width, height);

            GUI.Box(windowRect, "Dialogue Debug Console");

            GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 30, width - 20, height - 40));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target ID:", GUILayout.Width(70));
            TargetID = GUILayout.TextField(TargetID);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Play Target ID"))
            {
                PlayCurrentTarget();
                //_isVisible = false; 
            }

            GUILayout.Space(10);
            GUILayout.Label("Section Playback:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ep:", GUILayout.Width(25));
            TargetEpisode = GUILayout.TextField(TargetEpisode);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ch:", GUILayout.Width(25));
            TargetChapter = GUILayout.TextField(TargetChapter);
            GUILayout.Label("Sec:", GUILayout.Width(30));
            TargetSection = GUILayout.TextField(TargetSection);
            GUILayout.EndHorizontal();

            AutoAdvance = GUILayout.Toggle(AutoAdvance, "Auto Advance");

            if (GUILayout.Button("Play Section"))
            {
                PlaySection();
            }

            if (GUILayout.Button("Refresh List"))
            {
                RefreshIDList();
            }

            GUILayout.Space(10);
            GUILayout.Label("Available IDs:");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            foreach (var id in AvailableIDs)
            {
                if (GUILayout.Button(id))
                {
                    TargetID = id;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }
    }
}