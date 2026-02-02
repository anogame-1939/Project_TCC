using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.AnoNarrative.Editor
{
    [CustomEditor(typeof(DialogueController))]
    public class DialogueControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetEpisode;
        private SerializedProperty _targetChapter;
        private SerializedProperty _targetSection;
        private SerializedProperty _targetID;
        private SerializedProperty _playOnStart;
        private SerializedProperty _autoAdvance;

        private MasterDialogueData _masterData;
        private Vector2 _scrollPos;
        private bool _showCandidates = true;
        private List<string> _filteredIDs = new List<string>();
        private bool _useGridView = false;

        private void OnEnable()
        {
            _targetEpisode = serializedObject.FindProperty("TargetEpisode");
            _targetChapter = serializedObject.FindProperty("TargetChapter");
            _targetSection = serializedObject.FindProperty("TargetSection");
            _targetID = serializedObject.FindProperty("TargetID");
            _playOnStart = serializedObject.FindProperty("PlayOnStart");
            _autoAdvance = serializedObject.FindProperty("AutoAdvance");

            FindMasterData();
            UpdateFilteredList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_playOnStart);
            EditorGUILayout.PropertyField(_autoAdvance);

            EditorGUILayout.Space(10);

            // Selection (Moved to top)
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_targetID);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _targetID.stringValue = "";
            }
            GUILayout.EndHorizontal();

            // Validation Warning
            if (!string.IsNullOrEmpty(_targetID.stringValue))
            {
                if (_masterData != null)
                {
                    var exists = _masterData.Conversations.Any(c => c.ID == _targetID.stringValue);
                    if (!exists)
                    {
                        EditorGUILayout.HelpBox("Target ID not found in MasterDialogueData!", MessageType.Warning);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Please select a Target ID.", MessageType.Info);
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
                EditorGUILayout.HelpBox("MasterDialogueData not found in project. Cannot search conversations.", MessageType.Warning);
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
            // Create a left-aligned button style
            GUIStyle leftAlignedButtonStyle = new GUIStyle(GUI.skin.button);
            leftAlignedButtonStyle.alignment = TextAnchor.MiddleLeft;

            foreach (var id in _filteredIDs)
            {
                GUILayout.BeginHorizontal();
                if (id == _targetID.stringValue)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(id, leftAlignedButtonStyle))
                {
                    _targetID.stringValue = id;
                    GUI.FocusControl(null); // Remove focus to update field
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawGridCandidates()
        {
            float windowWidth = EditorGUIUtility.currentViewWidth - 40; // Approximate usable width
            float currentX = 0;

            // Create a left-aligned button style
            GUIStyle leftAlignedButtonStyle = new GUIStyle(GUI.skin.button);
            leftAlignedButtonStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginHorizontal();
            foreach (var id in _filteredIDs)
            {
                // Truncate logic: 10 chars 
                string display = id.Length > 10 ? id.Substring(0, 10) : id;
                GUIContent content = new GUIContent(display, id); // Tooltip has full ID

                // Keep button width roughly consistent or compact
                float btnWidth = 100f;

                if (currentX + btnWidth > windowWidth)
                {
                    currentX = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                if (id == _targetID.stringValue)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(content, leftAlignedButtonStyle, GUILayout.Width(btnWidth)))
                {
                    _targetID.stringValue = id;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;

                currentX += btnWidth + 4; // Spacing
            }
            GUILayout.EndHorizontal();
        }

        private void FindMasterData()
        {
            // Try to find via asset database
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
