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
        private bool _showRootsOnly = true;

        // Last Filter Values for force refresh
        private int LastEp, LastCh, LastSec;

        private const string PrefsKeyEp = "AnoNarrative_Filter_Ep";
        private const string PrefsKeyCh = "AnoNarrative_Filter_Ch";
        private const string PrefsKeySec = "AnoNarrative_Filter_Sec";

        public static void LoadFilters(out int ep, out int ch, out int sec)
        {
            ep = EditorPrefs.GetInt(PrefsKeyEp, -1);
            ch = EditorPrefs.GetInt(PrefsKeyCh, -1);
            sec = EditorPrefs.GetInt(PrefsKeySec, -1);
        }

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

            // Clear Button
            if (GUILayout.Button("Clear", GUILayout.Width(45)))
            {
                if (episodeProp != null) episodeProp.intValue = -1;
                if (chapterProp != null) chapterProp.intValue = -1;
                if (sectionProp != null) sectionProp.intValue = -1;
                GUI.FocusControl(null); // Unfocus any field
            }

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 25; // Short label width

            // Use custom filter fields
            DrawFilterField(episodeProp, "Ep");
            DrawFilterField(chapterProp, "Ch");
            DrawFilterField(sectionProp, "Sec");

            EditorGUIUtility.labelWidth = originalLabelWidth; // Restore
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList(
                    episodeProp != null ? episodeProp.intValue : -1,
                    chapterProp != null ? chapterProp.intValue : -1,
                    sectionProp != null ? sectionProp.intValue : -1
                );
            }

            EditorGUILayout.Space(10);
            DrawCandidates(convIDProp);
        }

        private void DrawFilterField(SerializedProperty prop, string label)
        {
            if (prop == null) return;

            int val = prop.intValue;
            string text = val == -1 ? "" : val.ToString();

            // Draw as Text Field to allow "Empty"
            string newText = EditorGUILayout.TextField(new GUIContent(label), text, GUILayout.MinWidth(50));

            if (newText != text)
            {
                if (string.IsNullOrEmpty(newText))
                {
                    prop.intValue = -1;
                }
                else
                {
                    if (int.TryParse(newText, out int result))
                    {
                        prop.intValue = result;
                    }
                }
            }
        }

        public void Initialize(int ep, int ch, int sec)
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
                }
                return;
            }

            // Options Line
            EditorGUILayout.BeginHorizontal();
            _showCandidates = EditorGUILayout.Foldout(_showCandidates, $"Candidates ({_filteredIDs.Count})", true);
            GUILayout.FlexibleSpace();
            bool newRootsValues = EditorGUILayout.ToggleLeft("Roots Only", _showRootsOnly, GUILayout.Width(85));
            if (newRootsValues != _showRootsOnly)
            {
                _showRootsOnly = newRootsValues;
                // Force update
                UpdateFilteredList(LastEp, LastCh, LastSec);
            }
            EditorGUILayout.EndHorizontal();

            if (_showCandidates)
            {
                // Toggle for Grid View
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Total Data: {_masterData.Conversations.Count}", EditorStyles.miniLabel);
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

                // Resolve Name
                string displayName = id;
                if (_masterData != null)
                {
                    var unit = _masterData.GetConversationByID(id);
                    if (unit != null && !string.IsNullOrEmpty(unit.SectionName))
                    {
                        displayName = unit.SectionName;
                    }
                }

                if (GUILayout.Button(displayName, leftAlignedButtonStyle))
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
                string displayName = id;
                string tooltip = id;
                if (_masterData != null)
                {
                    var unit = _masterData.GetConversationByID(id);
                    if (unit != null)
                    {
                        if (!string.IsNullOrEmpty(unit.SectionName))
                        {
                            displayName = unit.SectionName;
                        }
                        tooltip = $"{unit.SectionName}\n{unit.SpeakerName}\n{unit.BodyText}";
                    }
                }

                // Truncate logic: 10 chars 
                string displayLabel = displayName.Length > 12 ? displayName.Substring(0, 12) + ".." : displayName;
                GUIContent content = new GUIContent(displayLabel, tooltip);

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

        private void UpdateFilteredList(int ep, int ch, int sec)
        {
            LastEp = ep;
            LastCh = ch;
            LastSec = sec;

            _filteredIDs.Clear();
            if (_masterData == null) return;

            // Save to EditorPrefs
            EditorPrefs.SetInt(PrefsKeyEp, LastEp);
            EditorPrefs.SetInt(PrefsKeyCh, LastCh);
            EditorPrefs.SetInt(PrefsKeySec, LastSec);

            // 1. Identify all referenced IDs to find Roots (if needed) (Code remains same)
            HashSet<string> referencedIDs = new HashSet<string>();
            if (_showRootsOnly)
            {
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
            }

            // 2. Filter
            foreach (var conv in _masterData.Conversations)
            {
                if (_showRootsOnly && referencedIDs.Contains(conv.ID)) continue;

                bool match = true;

                // -1 implies "Empty" / "All"
                if (ep != -1 && conv.EpisodeID != ep) match = false;
                if (ch != -1 && conv.ChapterID != ch) match = false;
                if (sec != -1 && conv.SectionID != sec) match = false;

                if (match)
                {
                    _filteredIDs.Add(conv.ID);
                }
            }
        }
    }
}
