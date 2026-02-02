using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.AnoNarrative.Editor
{
    public class DialogueCandidateDrawer
    {
        private MasterDialogueData _masterData;
        private Vector2 _scrollPos;
        private bool _showCandidates = true;
        private List<string> _filteredIDs = new List<string>();
        private bool _useGridView = false;

        // Exposed for persistence if needed, or passed in via Draw
        // For simplicity, we'll keep local state here.

        public void Draw(
            SerializedProperty convIDProp,
            SerializedProperty episodeProp,
            SerializedProperty chapterProp,
            SerializedProperty sectionProp)
        {
            EnsureMasterData();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Filter Candidates", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // Horizontal Layout for Filters
            EditorGUILayout.BeginHorizontal();
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 25; // Short label width

            // Use passed properties
            if (episodeProp != null) EditorGUILayout.PropertyField(episodeProp, new GUIContent("Ep"), GUILayout.MinWidth(50));
            if (chapterProp != null) EditorGUILayout.PropertyField(chapterProp, new GUIContent("Ch"), GUILayout.MinWidth(50));
            if (sectionProp != null) EditorGUILayout.PropertyField(sectionProp, new GUIContent("Sc"), GUILayout.MinWidth(50));

            EditorGUIUtility.labelWidth = originalLabelWidth; // Restore
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList(episodeProp?.stringValue, chapterProp?.stringValue, sectionProp?.stringValue);
            }

            // Initial population if empty
            if (_filteredIDs.Count == 0 && _masterData != null)
            {
                // We intentionally don't auto-fill on every draw to avoid heavy linq, 
                // but we need it at least once. 
                // However, we rely on OnEnable usually. 
                // Since this is a helper, let's just check if we need to update.
                // Optimally the parent calls UpdateFilteredList on Enable.
            }

            EditorGUILayout.Space(10);
            DrawCandidates(convIDProp);
        }

        public void Initialize(string ep, string ch, string sec)
        {
            EnsureMasterData();
            UpdateFilteredList(ep, ch, sec);
        }

        private void EnsureMasterData()
        {
            if (_masterData != null) return;
            string[] guids = AssetDatabase.FindAssets("t:MasterDialogueData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _masterData = AssetDatabase.LoadAssetAtPath<MasterDialogueData>(path);
            }
        }

        private void DrawCandidates(SerializedProperty targetIDProp)
        {
            if (_masterData == null)
            {
                EditorGUILayout.HelpBox("MasterDialogueData not found. Cannot search conversations.", MessageType.Warning);
                if (GUILayout.Button("Retry Find MasterData"))
                {
                    EnsureMasterData();
                    // We don't have ease access to current filter values here unless stored or passed
                    // For now, let's assume they are stored in the SerializedProperties which we don't have direct access to right here without passing them again.
                    // Ideally, Initialize is called or Draw handles it.
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
                        DrawGridCandidates(targetIDProp);
                    }
                    else
                    {
                        DrawListCandidates(targetIDProp);
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawListCandidates(SerializedProperty targetIDProp)
        {
            GUIStyle leftAlignedButtonStyle = new GUIStyle(GUI.skin.button);
            leftAlignedButtonStyle.alignment = TextAnchor.MiddleLeft;

            foreach (var id in _filteredIDs)
            {
                GUILayout.BeginHorizontal();
                if (id == targetIDProp.stringValue)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(id, leftAlignedButtonStyle))
                {
                    targetIDProp.stringValue = id;
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawGridCandidates(SerializedProperty targetIDProp)
        {
            float windowWidth = EditorGUIUtility.currentViewWidth - 40;
            float currentX = 0;

            GUIStyle leftAlignedButtonStyle = new GUIStyle(GUI.skin.button);
            leftAlignedButtonStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginHorizontal();
            foreach (var id in _filteredIDs)
            {
                // Truncate logic: 10 chars 
                string display = id.Length > 10 ? id.Substring(0, 10) : id;
                GUIContent content = new GUIContent(display, id);

                float btnWidth = 100f;

                if (currentX + btnWidth > windowWidth)
                {
                    currentX = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                if (id == targetIDProp.stringValue)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(content, leftAlignedButtonStyle, GUILayout.Width(btnWidth)))
                {
                    targetIDProp.stringValue = id;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;

                currentX += btnWidth + 4;
            }
            GUILayout.EndHorizontal();
        }

        private void UpdateFilteredList(string ep, string ch, string sec)
        {
            _filteredIDs.Clear();
            if (_masterData == null) return;

            // 1. Identify all referenced IDs to find Roots
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
                if (referencedIDs.Contains(conv.ID)) continue;

                bool match = true;
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
