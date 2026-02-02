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
        public System.Action<int, int, int> OnSelectSection; // Ep, Ch, Sec

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
                var episodes = Data.Conversations.GroupBy(u => u.EpisodeID).OrderBy(g => g.Key);

                foreach (var epGroup in episodes)
                {
                    string epStr = epGroup.Key.ToString();
                    bool allowEp = string.IsNullOrEmpty(_searchFilter) || epStr.Contains(_searchFilter);

                    EditorGUILayout.LabelField($"Episode: {epGroup.Key}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    // Group by Chapter
                    var chapters = epGroup.GroupBy(u => u.ChapterID).OrderBy(g => g.Key);

                    foreach (var chapterGroup in chapters)
                    {
                        string chStr = chapterGroup.Key.ToString();
                        bool allowChapter = allowEp || chStr.Contains(_searchFilter);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{chapterGroup.Key}", EditorStyles.miniBoldLabel);
                        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            CreateSection(epGroup.Key, chapterGroup.Key);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel++;

                        // Group by Section
                        var sections = chapterGroup.GroupBy(u => u.SectionID).OrderBy(g => g.Key);

                        foreach (var sectionGroup in sections)
                        {
                            // Filter check
                            if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                            var first = sectionGroup.FirstOrDefault();
                            string displayName = string.IsNullOrEmpty(first?.SectionName) ? sectionGroup.Key.ToString() : first.SectionName;

                            if (GUILayout.Button($"{displayName} ({sectionGroup.Count()})", EditorStyles.miniButtonLeft))
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

        private void CreateSection(int epID, int chapterID)
        {
            // Find next Section ID
            int newSectionID = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID))
            {
                newSectionID = Data.Conversations.Where(u => u.EpisodeID == epID && u.ChapterID == chapterID).Max(u => u.SectionID) + 1;
            }

            // Suggest a Name?
            // Keeping it simple as per original logic which used "NewSection" text.
            // But now SectionID is int, so we don't rely on uniqueness of name for logic, just display.

            // Add initial node
            var newNode = new ConversationUnit
            {
                ID = $"{epID}_{chapterID}_{newSectionID}_1",
                EpisodeID = epID,
                ChapterID = chapterID,
                SectionID = newSectionID,
                SpeakerName = "New Speaker",
                BodyText = "Start",
                Position = new Vector2(100, 100)
            };
            Data.Conversations.Add(newNode);
        }

        private void CreateEpisode()
        {
            int newEp = 1;
            if (Data.Conversations.Any())
            {
                newEp = Data.Conversations.Max(u => u.EpisodeID) + 1;
            }

            // Create default chapter inside (Ch1)
            int ch = 1;

            CreateSection(newEp, ch);
        }

        private void DeleteSection(int ep, int chapter, int section)
        {
            if (EditorUtility.DisplayDialog("Delete Section", $"Are you sure you want to delete section '{section}' in '{ep}/{chapter}'?", "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section);
            }
        }
    }
}
