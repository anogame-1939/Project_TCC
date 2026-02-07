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
        private SerializedProperty _targetEpisode;
        private SerializedProperty _targetChapter;
        private SerializedProperty _targetSection;
        private SerializedProperty _conversationID;
        private SerializedProperty _pauseTimeline;
        private SerializedProperty _styleDatabase;
        private SerializedProperty _dialogueStyleName;

        private MasterDialogueData _masterData;
        private DialogueCandidateDrawer _drawer;

        private void OnEnable()
        {
            _targetEpisode = serializedObject.FindProperty("targetEpisode");
            _targetChapter = serializedObject.FindProperty("targetChapter");
            _targetSection = serializedObject.FindProperty("targetSection");
            _conversationID = serializedObject.FindProperty("conversationID");
            _pauseTimeline = serializedObject.FindProperty("pauseTimeline");
            _styleDatabase = serializedObject.FindProperty("styleDatabase");
            _dialogueStyleName = serializedObject.FindProperty("dialogueStyleName");

            _drawer = new DialogueCandidateDrawer();

            // If current values are all -1 (default), try to load from last session
            if (_targetEpisode.intValue == -1 && _targetChapter.intValue == -1 && _targetSection.intValue == -1)
            {
                DialogueCandidateDrawer.LoadFilters(out int ep, out int ch, out int sec);
                _targetEpisode.intValue = ep;
                _targetChapter.intValue = ch;
                _targetSection.intValue = sec;
                serializedObject.ApplyModifiedProperties(); // Apply immediately
            }

            _drawer.Initialize(_targetEpisode.intValue, _targetChapter.intValue, _targetSection.intValue);

            FindMasterData();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_pauseTimeline != null)
            {
                _pauseTimeline.boolValue = EditorGUILayout.Toggle("Pause Timeline", _pauseTimeline.boolValue);
            }

            if (_styleDatabase != null)
            {
                EditorGUILayout.PropertyField(_styleDatabase, new GUIContent("Style Database"));
                var db = (AnoGame.AnoNarrative.Data.DialogueStyle)_styleDatabase.objectReferenceValue;
                if (db == null)
                {
                    // Try auto-assign
                    string[] guids = AssetDatabase.FindAssets("t:DialogueStyle");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        db = AssetDatabase.LoadAssetAtPath<AnoGame.AnoNarrative.Data.DialogueStyle>(path);
                        if (db != null)
                        {
                            _styleDatabase.objectReferenceValue = db;
                        }
                    }

                    if (db == null && GUILayout.Button("Find Database"))
                    {
                        guids = AssetDatabase.FindAssets("t:DialogueStyle");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            _styleDatabase.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnoGame.AnoNarrative.Data.DialogueStyle>(path);
                        }
                    }
                }

                if (db != null)
                {
                    if (db.styleNames != null && db.styleNames.Count > 0)
                    {
                        var list = db.styleNames.ToList();
                        int index = list.IndexOf(_dialogueStyleName.stringValue);
                        if (index < 0) index = 0;

                        int newIndex = EditorGUILayout.Popup("Dialogue Style", index, list.ToArray());
                        if (newIndex >= 0 && newIndex < list.Count)
                        {
                            _dialogueStyleName.stringValue = list[newIndex];
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Database has no styles defined.");
                    }
                }
            }
            else
            {
                // Should we find one automatically?
                var guids = AssetDatabase.FindAssets("t:DialogueStyle");
                if (guids.Length > 0)
                {
                    // Found one, maybe suggest or auto-assign?
                    // For now just show field
                }
                EditorGUILayout.PropertyField(_styleDatabase, new GUIContent("Style Database"));
            }

            EditorGUILayout.Space(10);

            // Selection (Moved to top)
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            if (_conversationID != null)
            {
                // Show current ID
                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(_conversationID);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    _conversationID.stringValue = "";
                }
                GUILayout.EndHorizontal();

                // Show resolved Section Name
                if (!string.IsNullOrEmpty(_conversationID.stringValue) && _masterData != null)
                {
                    var unit = _masterData.GetConversationByID(_conversationID.stringValue);
                    if (unit != null)
                    {
                        EditorGUILayout.HelpBox($"Selected: {unit.SectionName} (Speaker: {unit.SpeakerName})", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("ID not found in MasterData", MessageType.Warning);
                    }
                }
            }

            // Validation Checking
            if (_conversationID != null && !string.IsNullOrEmpty(_conversationID.stringValue))
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
            else
            {
                EditorGUILayout.HelpBox("Please select a Conversation ID.", MessageType.Info);
            }

            // Use Shared Drawer
            if (_drawer != null && _conversationID != null && _targetEpisode != null && _targetChapter != null && _targetSection != null)
            {
                _drawer.Draw(_conversationID, _targetEpisode, _targetChapter, _targetSection);
            }

            serializedObject.ApplyModifiedProperties();
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
    }
}
