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
        public System.Action<string, string> OnSelectSection;

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
                // Group by Chapter first
                var chapters = Data.Conversations.GroupBy(u => u.ChapterID ?? "Default").OrderBy(g => g.Key);

                foreach (var chapterGroup in chapters)
                {
                    bool allowChapter = string.IsNullOrEmpty(_searchFilter) || chapterGroup.Key.ToLower().Contains(_searchFilter.ToLower());

                    EditorGUILayout.LabelField($"{chapterGroup.Key}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    // Group by Section
                    var sections = chapterGroup.GroupBy(u => u.SectionID ?? "General").OrderBy(g => g.Key);

                    foreach (var sectionGroup in sections)
                    {
                        if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                        if (GUILayout.Button($"{sectionGroup.Key} ({sectionGroup.Count()})", EditorStyles.miniButtonLeft))
                        {
                            // Trigger Filter
                            OnSelectSection?.Invoke(chapterGroup.Key, sectionGroup.Key);

                            // Pan to first item
                            var first = sectionGroup.FirstOrDefault();
                            if (first != null) OnRequestPanTo?.Invoke(first.Position);
                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
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
    }
}
