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

        private MasterDialogueData _masterData;
        private DialogueCandidateDrawer _drawer;

        private void OnEnable()
        {
            _targetEpisode = serializedObject.FindProperty("targetEpisode");
            _targetChapter = serializedObject.FindProperty("targetChapter");
            _targetSection = serializedObject.FindProperty("targetSection");
            _conversationID = serializedObject.FindProperty("conversationID");
            _pauseTimeline = serializedObject.FindProperty("pauseTimeline");

            _drawer = new DialogueCandidateDrawer();
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

            EditorGUILayout.Space(10);

            // Selection (Moved to top)
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            if (_conversationID != null)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_conversationID);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    _conversationID.stringValue = "";
                }
                GUILayout.EndHorizontal();
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
