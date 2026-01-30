using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Systems.Dialogue.Debug
{
    public class DialogueDebugController : MonoBehaviour
    {
        [Header("Runtime Debug")]
        [Tooltip("Available Conversation IDs (auto-filled if MasterData is accessible via Manager)")]
        public List<string> AvailableIDs = new List<string>();

        [Tooltip("ID to play when 'Play' button is clicked")]
        public string TargetID;

        [Header("On-Screen Console")]
        public bool ShowOnScreen = false;
        public KeyCode ToggleKey = KeyCode.BackQuote; // "~" key

        private bool _isVisible = false;
        private Vector2 _scrollPos;

        private void Start()
        {
            RefreshIDList();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _isVisible = !_isVisible;
                if (_isVisible) RefreshIDList();
            }
        }

        public void PlayCurrentTarget()
        {
            if (string.IsNullOrEmpty(TargetID))
            {
                UnityEngine.Debug.LogWarning("[DialogueDebug] No TargetID specified.");
                return;
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartConversation(TargetID);
            }
            else
            {
                UnityEngine.Debug.LogError("[DialogueDebug] DialogueManager instance not found.");
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
                var mgr = FindObjectOfType<DialogueManager>();
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
                _isVisible = false; // Close on play? Or keep open? Let's keep open.
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
