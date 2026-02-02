using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using AnoGame.AnoNarrative.Timeline;

namespace AnoGame.AnoNarrative.Editor
{
    [CustomEditor(typeof(AnoNarrativeClip))]
    public class AnoNarrativeClipEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetController;
        private SerializedProperty _targetEpisode;
        private SerializedProperty _targetChapter;
        private SerializedProperty _targetSection;
        private SerializedProperty _conversationID;
        private SerializedProperty _pauseTimeline;

        private MasterDialogueData _masterData;
        private Vector2 _scrollPos;
        private bool _showCandidates = true;
        private List<string> _filteredIDs = new List<string>();
        private bool _useGridView = false;

        private void OnEnable()
        {
            _targetController = serializedObject.FindProperty("targetController");
            _targetEpisode = serializedObject.FindProperty("targetEpisode");
            _targetChapter = serializedObject.FindProperty("targetChapter");
            _targetSection = serializedObject.FindProperty("targetSection");
            _conversationID = serializedObject.FindProperty("conversationID");
            _pauseTimeline = serializedObject.FindProperty("pauseTimeline");

            FindMasterData();
            UpdateFilteredList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_targetController);
            EditorGUILayout.PropertyField(_pauseTimeline);

            EditorGUILayout.Space(10);

            // Selection (Moved to top)
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_conversationID);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _conversationID.stringValue = "";
            }
            GUILayout.EndHorizontal();

            // Validation Checking
            if (!string.IsNullOrEmpty(_conversationID.stringValue))
            {
                if (_masterData != null)
                {
                    var exists = _masterData.Conversations.Any(c => c.ID == _conversationID.stringValue);
                    if (!exists)
                    {
                        EditorGUILayout.HelpBox("Conversation ID not found in MasterDialogueData!", MessageType.Warning);
                    }
                }
            }
            else if (_targetController.exposedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Please select a Conversation ID.", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Filter Candidates", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // Horizontal Layout for Filters
            EditorGUILayout.BeginHorizontal();
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 25; // Short label width

            EditorGUILayout.PropertyField(_targetEpisode, new GUIContent("Ep"), GUILayout.MinWidth(50));
            EditorGUILayout.PropertyField(_targetChapter, new GUIContent("Ch"), GUILayout.MinWidth(50));
            EditorGUILayout.PropertyField(_targetSection, new GUIContent("Sc"), GUILayout.MinWidth(50));

            EditorGUIUtility.labelWidth = originalLabelWidth; // Restore
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList();
            }

            EditorGUILayout.Space(10);
            DrawCandidates();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCandidates()
        {
            if (_masterData == null)
            {
                EditorGUILayout.HelpBox("MasterDialogueData not found. Cannot search conversations.", MessageType.Warning);
                if (GUILayout.Button("Retry Find MasterData"))
                {
                    FindMasterData();
                    UpdateFilteredList();
                }
                return;
            }

            _showCandidates = EditorGUILayout.Foldout(_showCandidates, $"Candidates ({_filteredIDs.Count}) - Roots Only", true);
            if (_showCandidates)
            {
                // Toggle for Grid View
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                _useGridView = GUILayout.Toggle(_useGridView, "Grid View", EditorStyles.miniButton, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();

                if (_filteredIDs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No conversations match the current filter.", MessageType.Info);
                }
                else
                {
                    float height = Mathf.Min(_filteredIDs.Count * (_useGridView ? 30f : 22f), 200f);
                    if (_useGridView) height = 200f; // Fixed height for grid

                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(height));

                    if (_useGridView)
                    {
                        DrawGridCandidates();
                    }
                    else
                    {
                        DrawListCandidates();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawListCandidates()
        {
            foreach (var id in _filteredIDs)
            {
                GUILayout.BeginHorizontal();
                if (id == _conversationID.stringValue)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(id))
                {
                    _conversationID.stringValue = id;
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawGridCandidates()
        {
            float windowWidth = EditorGUIUtility.currentViewWidth - 40; // Approximate usable width
            float currentX = 0;

            GUILayout.BeginHorizontal();
            foreach (var id in _filteredIDs)
            {
                // Truncate logic: 5 chars 
                string display = id.Length > 5 ? id.Substring(0, 5) : id;
                GUIContent content = new GUIContent(display, id); // Tooltip has full ID

                // Keep button width roughly consistent or compact
                float btnWidth = 50f;

                if (currentX + btnWidth > windowWidth)
                {
                    currentX = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                if (id == _conversationID.stringValue)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(content, GUILayout.Width(btnWidth)))
                {
                    _conversationID.stringValue = id;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;

                currentX += btnWidth + 4; // Spacing
            }
            GUILayout.EndHorizontal();
        }

        private void FindMasterData()
        {
            string[] guids = AssetDatabase.FindAssets("t:MasterDialogueData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _masterData = AssetDatabase.LoadAssetAtPath<MasterDialogueData>(path);
            }
        }

        private void UpdateFilteredList()
        {
            _filteredIDs.Clear();
            if (_masterData == null) return;

            string ep = _targetEpisode.stringValue;
            string ch = _targetChapter.stringValue;
            string sec = _targetSection.stringValue;

            // 1. Identify all referenced IDs to find Roots
            // A Root is a ConversationUnit that is NOT referenced by any other unit's NextID or Choices.
            HashSet<string> referencedIDs = new HashSet<string>();
            foreach (var unit in _masterData.Conversations)
            {
                if (!string.IsNullOrEmpty(unit.NextID)) referencedIDs.Add(unit.NextID);
                if (unit.Choices != null)
                {
                    foreach (var choice in unit.Choices)
                    {
                        if (!string.IsNullOrEmpty(choice.TargetID)) referencedIDs.Add(choice.TargetID);
                    }
                }
            }

            // 2. Filter
            foreach (var conv in _masterData.Conversations)
            {
                // Root Check
                if (referencedIDs.Contains(conv.ID)) continue;

                bool match = true;
                // Empty filter string matches everything
                if (!string.IsNullOrEmpty(ep) && conv.EpisodeID != ep) match = false;
                if (!string.IsNullOrEmpty(ch) && conv.ChapterID != ch) match = false;
                if (!string.IsNullOrEmpty(sec) && conv.SectionID != sec) match = false;

                if (match)
                {
                    _filteredIDs.Add(conv.ID);
                }
            }
        }
    }
}
