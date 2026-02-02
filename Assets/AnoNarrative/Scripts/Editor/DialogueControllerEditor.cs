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
        private DialogueCandidateDrawer _drawer;

        private void OnEnable()
        {
            _targetEpisode = serializedObject.FindProperty("TargetEpisode");
            _targetChapter = serializedObject.FindProperty("TargetChapter");
            _targetSection = serializedObject.FindProperty("TargetSection");
            _targetID = serializedObject.FindProperty("TargetID");
            _playOnStart = serializedObject.FindProperty("PlayOnStart");
            _autoAdvance = serializedObject.FindProperty("AutoAdvance");

            _drawer = new DialogueCandidateDrawer();
            _drawer.Initialize(_targetEpisode.intValue, _targetChapter.intValue, _targetSection.intValue);

            FindMasterData();
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

            if (_drawer != null)
            {
                _drawer.Draw(_targetID, _targetEpisode, _targetChapter, _targetSection);
            }

            serializedObject.ApplyModifiedProperties();
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
    }
}
