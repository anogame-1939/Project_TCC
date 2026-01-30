using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Systems.Dialogue.Editor
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

                    EditorGUILayout.LabelField($"Episode: {epGroup.Key}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    // Group by Chapter
                    var chapters = epGroup.GroupBy(u => u.ChapterID ?? "Default").OrderBy(g => g.Key);

                    foreach (var chapterGroup in chapters)
                    {
                        bool allowChapter = allowEp || chapterGroup.Key.ToLower().Contains(_searchFilter.ToLower());

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{chapterGroup.Key}", EditorStyles.miniBoldLabel);
                        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            CreateSection(epGroup.Key, chapterGroup.Key);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel++;

                        // Group by Section
                        var sections = chapterGroup.GroupBy(u => u.SectionID ?? "General").OrderBy(g => g.Key);

                        foreach (var sectionGroup in sections)
                        {
                            if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                            if (GUILayout.Button($"{sectionGroup.Key} ({sectionGroup.Count()})", EditorStyles.miniButtonLeft))
                            {
                                // Trigger Filter
                                OnSelectSection?.Invoke(epGroup.Key, chapterGroup.Key, sectionGroup.Key);

                                // Pan to first item
                                var first = sectionGroup.FirstOrDefault();
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

        private void CreateSection(string epID, string chapterID)
        {
            string newSection = "NewSection";
            int count = 1;
            while (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID && u.SectionID == newSection))
            {
                newSection = $"NewSection_{count++}";
            }

            // Add initial node
            var newNode = new ConversationUnit
            {
                ID = $"{epID}_{chapterID}_{newSection}_1",
                EpisodeID = epID,
                ChapterID = chapterID,
                SectionID = newSection,
                SpeakerName = "New Speaker",
                BodyText = "Start",
                Position = new Vector2(100, 100)
            };
            Data.Conversations.Add(newNode);
        }

        private void CreateEpisode()
        {
            string newEp = "Ep1";
            int count = 1;
            while (Data.Conversations.Any(u => u.EpisodeID == newEp))
            {
                newEp = $"Ep{++count}";
            }
            // Create default chapter inside
            string ch = "Ch1";

            CreateSection(newEp, ch);
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
