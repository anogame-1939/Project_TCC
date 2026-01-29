using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Systems.Dialogue.Editor
{
    [CustomEditor(typeof(MasterDialogueData))]
    public class MasterDialogueDataEditor : UnityEditor.Editor
    {
        private MasterDialogueData _target;
        private string _searchString = "";
        private Vector2 _scrollPos;
        private bool _showDetails = true;

        private void OnEnable()
        {
            _target = (MasterDialogueData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue Database Management", EditorStyles.boldLabel);

            // Search Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchString = EditorGUILayout.TextField(_searchString);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _searchString = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Clear All Button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All Data", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All Data", "Are you sure you want to delete ALL conversation data? This cannot be undone.", "Yes, Delete All", "Cancel"))
                {
                    _target.Conversations.Clear();
                    EditorUtility.SetDirty(_target);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            // Add New Button
            if (GUILayout.Button("Add New Conversation", GUILayout.Height(30)))
            {
                AddNewConversation();
            }

            EditorGUILayout.Space();

            // Filtered List
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400)); // Fixed height for scroll view

            if (_target.Conversations == null) _target.Conversations = new List<ConversationUnit>();

            for (int i = 0; i < _target.Conversations.Count; i++)
            {
                var unit = _target.Conversations[i];

                // Filter logic
                if (!string.IsNullOrEmpty(_searchString))
                {
                    bool matchID = unit.ID != null && unit.ID.ToLower().Contains(_searchString.ToLower());
                    bool matchText = unit.BodyText != null && unit.BodyText.ToLower().Contains(_searchString.ToLower());
                    bool matchSpeaker = unit.SpeakerName != null && unit.SpeakerName.ToLower().Contains(_searchString.ToLower());

                    if (!matchID && !matchText && !matchSpeaker) continue;
                }

                DrawConversationUnit(unit, i);
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConversationUnit(ConversationUnit unit, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{index}] ID: {unit.ID}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Conversation", $"Are you sure you want to delete {unit.ID}?", "Yes", "No"))
                {
                    _target.Conversations.RemoveAt(index);
                    return; // Exit to avoid index out of range
                }
            }
            EditorGUILayout.EndHorizontal();

            // Content
            unit.ID = EditorGUILayout.TextField("ID", unit.ID);
            unit.SpeakerName = EditorGUILayout.TextField("Speaker", unit.SpeakerName);

            EditorGUILayout.LabelField("Body Text:");
            unit.BodyText = EditorGUILayout.TextArea(unit.BodyText, GUILayout.Height(50));

            unit.NextID = EditorGUILayout.TextField("Next ID", unit.NextID);

            // Choices (Simplified view)
            if (unit.Choices == null) unit.Choices = new List<Choice>();

            SerializedProperty conversationsProp = serializedObject.FindProperty("Conversations");
            if (index < conversationsProp.arraySize)
            {
                SerializedProperty unitProp = conversationsProp.GetArrayElementAtIndex(index);
                SerializedProperty choicesProp = unitProp.FindPropertyRelative("Choices");
                EditorGUILayout.PropertyField(choicesProp, new GUIContent("Choices"), true);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void AddNewConversation()
        {
            var newUnit = new ConversationUnit();
            newUnit.ID = "New_Conversation_" + _target.Conversations.Count;
            _target.Conversations.Add(newUnit);
        }
    }
}
