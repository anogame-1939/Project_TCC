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
            EditorGUILayout.LabelField("Filter Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_targetEpisode);
            EditorGUILayout.PropertyField(_targetChapter);
            EditorGUILayout.PropertyField(_targetSection);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList();
            }

            EditorGUILayout.Space(10);
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

            _showCandidates = EditorGUILayout.Foldout(_showCandidates, $"Candidates ({_filteredIDs.Count})", true);
            if (_showCandidates)
            {
                if (_filteredIDs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No conversations match the current filter.", MessageType.Info);
                }
                else
                {
                    // Limit height for scroll view
                    float height = Mathf.Min(_filteredIDs.Count * 22f, 200f);
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(height));

                    foreach (var id in _filteredIDs)
                    {
                        GUILayout.BeginHorizontal();
                        if (id == _targetID.stringValue)
                        {
                            GUI.backgroundColor = Color.green;
                        }

                        if (GUILayout.Button(id))
                        {
                            _targetID.stringValue = id;
                            GUI.FocusControl(null); // Remove focus to update field
                        }

                        GUI.backgroundColor = Color.white;
                        GUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
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

            foreach (var conv in _masterData.Conversations)
            {
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
