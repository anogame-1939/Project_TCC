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

            // Migration
            if (_target.ActorList != null && _target.ActorList.Count > 0)
            {
                if (_target.ActorDefinitions == null) _target.ActorDefinitions = new List<ActorDefinition>();

                int colorIndex = _target.ActorDefinitions.Count;
                foreach (var name in _target.ActorList)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        var def = new ActorDefinition();
                        def.Name = name;
                        def.Color = MasterDialogueData.PastelPalette[colorIndex % MasterDialogueData.PastelPalette.Length];
                        _target.ActorDefinitions.Add(def);
                        colorIndex++;
                    }
                }
                _target.ActorList.Clear();
                EditorUtility.SetDirty(_target);
            }
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

            // Migration Logic
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Migration Tools", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Migrate Hierarchy from IDs", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Migrate Hierarchy", "This will try to populate Episode/Chapter/Section fields from existing IDs (format: Ep_Ch_Sec_Num). Continue?", "Yes", "Cancel"))
                {
                    MigrateHierarchy();
                }
            }
            if (GUILayout.Button("Migrate 0.0.0 Format", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Migrate 0.0.0 Format", "This will parse ChapterID '0.0.0' into Ep/Ch/Sec, and move current SectionID to SectionName. Continue?", "Yes", "Cancel"))
                {
                    MigrateLegacyFormat();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Actor Management
            DrawActorManagement();

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

            EditorGUILayout.BeginHorizontal();
            unit.EpisodeID = EditorGUILayout.TextField("Ep", unit.EpisodeID);
            unit.ChapterID = EditorGUILayout.TextField("Ch", unit.ChapterID);
            unit.SectionID = EditorGUILayout.TextField("Sec", unit.SectionID);
            EditorGUILayout.EndHorizontal();

            unit.SectionName = EditorGUILayout.TextField("Section Name", unit.SectionName);
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

        private void DrawActorManagement()
        {
            EditorGUILayout.LabelField("Actor Management", EditorStyles.boldLabel);

            if (_target.ActorDefinitions == null) _target.ActorDefinitions = new List<ActorDefinition>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Theme Selector
            EditorGUILayout.BeginHorizontal();
            ColorTheme newTheme = (ColorTheme)EditorGUILayout.EnumPopup("Color Theme", _target.Theme);
            if (newTheme != _target.Theme)
            {
                _target.Theme = newTheme;
                EditorUtility.SetDirty(_target);
            }
            if (GUILayout.Button("Apply Theme to All", GUILayout.Width(130)))
            {
                if (EditorUtility.DisplayDialog("Apply Theme", "This will overwrite all actor colors based on the selected theme. Continue?", "Yes", "No"))
                {
                    var palette = MasterDialogueData.GetPalette(_target.Theme);
                    for (int i = 0; i < _target.ActorDefinitions.Count; i++)
                    {
                        _target.ActorDefinitions[i].Color = palette[i % palette.Length];
                    }
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            for (int i = 0; i < _target.ActorDefinitions.Count; i++)
            {
                var actor = _target.ActorDefinitions[i];
                EditorGUILayout.BeginHorizontal();

                // Color Picker
                Color newColor = EditorGUILayout.ColorField(GUIContent.none, actor.Color, false, false, false, GUILayout.Width(40));
                if (newColor != actor.Color)
                {
                    actor.Color = newColor;
                    EditorUtility.SetDirty(_target);
                }

                // Name
                string newName = EditorGUILayout.TextField(actor.Name);
                if (newName != actor.Name)
                {
                    actor.Name = newName;
                    EditorUtility.SetDirty(_target);
                }

                // Remove
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _target.ActorDefinitions.RemoveAt(i);
                    EditorUtility.SetDirty(_target);
                    i--; // adjust index
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add New Actor"))
            {
                var newActor = new ActorDefinition();
                newActor.Name = "New Actor";
                // Auto-assign color based on Theme
                var palette = MasterDialogueData.GetPalette(_target.Theme);
                newActor.Color = palette[_target.ActorDefinitions.Count % palette.Length];

                _target.ActorDefinitions.Add(newActor);
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNewConversation()
        {
            var newUnit = new ConversationUnit();
            newUnit.ID = "New_Conversation_" + _target.Conversations.Count;
            _target.Conversations.Add(newUnit);
        }

        private void MigrateHierarchy()
        {
            Undo.RecordObject(_target, "Migrate Dialogue Hierarchy");
            foreach (var unit in _target.Conversations)
            {
                if (string.IsNullOrEmpty(unit.ID)) continue;

                var parts = unit.ID.Split('_');
                if (parts.Length == 4)
                {
                    unit.EpisodeID = parts[0];
                    unit.ChapterID = parts[1];
                    unit.SectionID = parts[2];
                }
                else if (parts.Length == 3)
                {
                    unit.EpisodeID = "Default";
                    unit.ChapterID = parts[0];
                    unit.SectionID = parts[1];
                }
                else if (parts.Length == 2)
                {
                    unit.EpisodeID = "Default";
                    unit.ChapterID = "Default";
                    unit.SectionID = parts[0];
                }
                else
                {
                    unit.EpisodeID = "Default";
                    unit.ChapterID = "Default";
                    unit.SectionID = unit.ID;
                }
            }
            EditorUtility.SetDirty(_target);
            UnityEngine.Debug.Log($"[DialogueSystem] Migrated {_target.Conversations.Count} units.");
        }

        private void MigrateLegacyFormat()
        {
            Undo.RecordObject(_target, "Migrate Dialogue Legacy Format");
            int count = 0;
            foreach (var unit in _target.Conversations)
            {
                // 1. Move current SectionID to SectionName (since it contains the name)
                if (!string.IsNullOrEmpty(unit.SectionID) && string.IsNullOrEmpty(unit.SectionName))
                {
                    unit.SectionName = unit.SectionID;
                }

                // 2. Parse 0.0.0 from ChapterID if it exists in that format
                if (!string.IsNullOrEmpty(unit.ChapterID) && unit.ChapterID.Contains("."))
                {
                    var dots = unit.ChapterID.Split('.');
                    if (dots.Length == 3)
                    {
                        unit.EpisodeID = dots[0];
                        unit.ChapterID = dots[1];
                        unit.SectionID = dots[2];
                    }
                }
                count++;
            }
            EditorUtility.SetDirty(_target);
            UnityEngine.Debug.Log($"[DialogueSystem] Legacy Migration complete for {count} units.");
        }
    }
}
