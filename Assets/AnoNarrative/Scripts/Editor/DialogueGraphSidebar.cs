using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.AnoNarrative.Editor
{
    public class DialogueGraphSidebar
    {
        public MasterDialogueData Data;
        private Vector2 _scrollPos;
        private string _searchFilter = "";

        // Navigation events
        public System.Action<Vector2> OnRequestPanTo;
        public System.Action<string, string, string> OnSelectSection; // Ep, Ch, Sec

        public DialogueGraphSidebar(MasterDialogueData data)
        {
            Data = data;
        }

        public void Draw(float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Navigator", EditorStyles.boldLabel);

            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (Data != null)
            {
                // Group by Episode first
                var episodes = Data.Conversations.GroupBy(u => u.EpisodeID ?? "Default").OrderBy(g => g.Key);

                foreach (var epGroup in episodes)
                {
                    bool allowEp = string.IsNullOrEmpty(_searchFilter) || epGroup.Key.ToLower().Contains(_searchFilter.ToLower());

                    EditorGUILayout.BeginHorizontal();
                    // Left-aligned bold label for Episode
                    GUILayout.Label($"Episode: {epGroup.Key}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    // Add Button for creating new Chapter in this Episode
                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        CreateChapter(epGroup.Key);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;

                    // Group by Chapter
                    var chapters = epGroup.GroupBy(u => u.ChapterID ?? "Default").OrderBy(g => g.Key);

                    foreach (var chapterGroup in chapters)
                    {
                        bool allowChapter = allowEp || chapterGroup.Key.ToLower().Contains(_searchFilter.ToLower());

                        // Chapter Header with Add Section Button
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label($"Chapter: {chapterGroup.Key}", EditorStyles.miniBoldLabel); // Changed format
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            RequestCreateSection(epGroup.Key, chapterGroup.Key);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel++;

                        // Group by Section
                        var sections = chapterGroup.GroupBy(u => u.SectionID ?? "General").OrderBy(g => g.Key);

                        foreach (var sectionGroup in sections)
                        {
                            if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                            var first = sectionGroup.FirstOrDefault();
                            string displayName = string.IsNullOrEmpty(first?.SectionName) ? sectionGroup.Key : first.SectionName;

                            // Left Aligned Section Button
                            GUIStyle leftButton = new GUIStyle(EditorStyles.miniButtonLeft);
                            leftButton.alignment = TextAnchor.MiddleLeft;

                            if (GUILayout.Button($"{displayName} ({sectionGroup.Count()})", leftButton))
                            {
                                // Trigger Filter
                                OnSelectSection?.Invoke(epGroup.Key, chapterGroup.Key, sectionGroup.Key);

                                // Pan to first item
                                if (first != null) OnRequestPanTo?.Invoke(first.Position);
                            }

                            // Context Menu for Section
                            Rect btnRect = GUILayoutUtility.GetLastRect();
                            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && btnRect.Contains(Event.current.mousePosition))
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Delete Section"), false, () => DeleteSection(epGroup.Key, chapterGroup.Key, sectionGroup.Key));
                                menu.ShowAsContext();
                                Event.current.Use();
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Add Episode"))
                {
                    CreateEpisode();
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Draw Divider line
            Rect divider = GUILayoutUtility.GetLastRect();
            divider.x += divider.width;
            divider.width = 1;
            EditorGUI.DrawRect(divider, Color.black);
        }

        private void RequestCreateSection(string epID, string chapterID)
        {
            // Suggest a default name
            string defaultName = GetUniqueSectionName(epID, chapterID);

            // Show Popup
            TextInputPopup.Show(
                "Create Section",
                "Enter a name for the new Section (leave empty for default):",
                "",
                defaultName,
                (result) => CreateSection(epID, chapterID, result)
            );
        }

        private string GetUniqueSectionName(string epID, string chapterID)
        {
            string baseName = "NewSection";
            int count = 1;
            string candidate = baseName;
            while (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID && u.SectionID == candidate))
            {
                candidate = $"{baseName}_{count++}";
            }
            return candidate;
        }

        private void CreateSection(string epID, string chapterID, string sectionName)
        {
            // Ensure unique if user typed something that already exists? 
            // For now, let's just create it. Ideally we might want to check for duplicates again, 
            // but the graph can handle multiple sections with same name if they are distinct logic blocks technically?
            // Actually, usually SectionID should be unique within the Chapter.

            // Validation: IF exists, maybe append suffix?
            if (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID && u.SectionID == sectionName))
            {
                sectionName = GetUniqueSectionName(epID, chapterID); // Fallback to unique
            }

            // Add initial node
            var newNode = new ConversationUnit
            {
                ID = $"{epID}_{chapterID}_{sectionName}_1",
                EpisodeID = epID,
                ChapterID = chapterID,
                SectionID = sectionName,
                SectionName = sectionName, // Also set SectionName
                SpeakerName = "New Speaker",
                BodyText = "Start",
                Position = new Vector2(100, 100)
            };
            Data.Conversations.Add(newNode);
        }

        private void CreateChapter(string epID)
        {
            string newCh = "Ch1";
            int count = 1;
            while (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == newCh))
            {
                newCh = $"Ch{++count}";
            }

            RequestCreateSection(epID, newCh);
        }

        private void CreateEpisode()
        {
            string newEp = "Ep1";
            int count = 1;
            while (Data.Conversations.Any(u => u.EpisodeID == newEp))
            {
                newEp = $"Ep{++count}";
            }

            CreateChapter(newEp);
        }

        private void DeleteSection(string ep, string chapter, string section)
        {
            if (EditorUtility.DisplayDialog("Delete Section", $"Are you sure you want to delete section '{section}' in '{ep}/{chapter}'?", "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section);
            }
        }
    }
}
