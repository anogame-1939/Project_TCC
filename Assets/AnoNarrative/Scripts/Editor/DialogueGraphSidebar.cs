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
        public System.Action<int, int, int, string> OnSelectSection;

        // Static変数でドラッグデータを保持
        private static SidebarDragData _currentDragData;

        public DialogueGraphSidebar(MasterDialogueData data)
        {
            Data = data;
        }

        public void Draw(float width)
        {
            // イベント処理：ドラッグ終了や中断時の強制リセット
            Event evt = Event.current;
            if (evt.type == EventType.DragExited || evt.type == EventType.Ignore || (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape))
            {
                if (_currentDragData != null)
                {
                    _currentDragData = null;
                }
            }
            // マウスアップ時も、ドラッグ中ならリセット
            if (evt.type == EventType.MouseUp && _currentDragData != null)
            {
                // GUIループの最後に処理されるべきだが、簡易対応として
            }

            GUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Navigator", EditorStyles.boldLabel);

            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (Data != null)
            {
                var episodes = Data.Conversations.GroupBy(u => u.EpisodeID).OrderBy(g => g.Key);

                foreach (var epGroup in episodes)
                {
                    string epStr = epGroup.Key == -1 ? "Default" : epGroup.Key.ToString();
                    bool allowEp = string.IsNullOrEmpty(_searchFilter) || epStr.Contains(_searchFilter);

                    // --- EPISODE Header ---
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Episode: {epStr}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        CreateChapter(epGroup.Key);
                    }
                    EditorGUILayout.EndHorizontal();

                    // Episode Drop Zone (allow dropping Chapter to end of Episode)
                    Rect epRect = GUILayoutUtility.GetLastRect();
                    if (_currentDragData != null && _currentDragData.Type == DragType.Chapter)
                    {
                        if (epRect.Contains(evt.mousePosition))
                        {
                            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                if (evt.type == EventType.DragUpdated) Event.current.Use();
                                if (evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();
                                    // Append to end of this Episode
                                    int targetCh = 1;
                                    if (epGroup.Any()) targetCh = epGroup.Max(u => u.ChapterID) + 1;
                                    PerformChapterReorder(_currentDragData, epGroup.Key, targetCh);
                                    _currentDragData = null;
                                    Event.current.Use();
                                }
                            }
                        }
                        // Visual feedback for Episode drop
                        if (evt.type == EventType.Repaint && epRect.Contains(evt.mousePosition))
                        {
                            EditorGUI.DrawRect(epRect, new Color(0, 1, 1, 0.2f));
                        }
                    }

                    EditorGUI.indentLevel++;

                    var chapters = epGroup.GroupBy(u => u.ChapterID).OrderBy(g => g.Key);

                    foreach (var chapterGroup in chapters)
                    {
                        string chStr = chapterGroup.Key == -1 ? "Default" : chapterGroup.Key.ToString();
                        bool allowChapter = allowEp || chStr.Contains(_searchFilter);

                        // =========================================================
                        // Chapter Header Logic
                        // =========================================================
                        Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        float headerWidth = width;

                        // 1. Label Drawing & Interact Rect
                        Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - 50, headerRect.height);
                        EditorGUI.LabelField(labelRect, $"{chStr}", EditorStyles.miniBoldLabel);

                        // 2. Drag Start (Chapter)
                        // Only allow dragging if NOT dragging something else
                        if (_currentDragData == null && evt.type == EventType.MouseDrag && labelRect.Contains(evt.mousePosition))
                        {
                            Debug.Log($"Drag Start Chapter: {chStr}");
                            _currentDragData = new SidebarDragData { Type = DragType.Chapter, Ep = epGroup.Key, Ch = chapterGroup.Key };
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.SetGenericData("ChapterDrag", _currentDragData);
                            DragAndDrop.objectReferences = new UnityEngine.Object[0];
                            DragAndDrop.StartDrag($"Move Chapter {chStr}");
                            Event.current.Use();
                        }

                        // 3. Drop Zone Logic (Chapter Reorder)
                        if (_currentDragData != null && _currentDragData.Type == DragType.Chapter)
                        {
                            // Define Zones
                            Rect chDropRect = new Rect(0, headerRect.y, headerWidth, headerRect.height);
                            Rect chTopZone = new Rect(0, headerRect.y, headerWidth, headerRect.height * 0.5f);
                            Rect chBotZone = new Rect(0, headerRect.y + (headerRect.height * 0.5f), headerWidth, headerRect.height * 0.5f);

                            bool isInChRect = chDropRect.Contains(evt.mousePosition);
                            bool isTop = chTopZone.Contains(evt.mousePosition);

                            if (isInChRect)
                            {
                                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                    if (evt.type == EventType.DragUpdated) Event.current.Use();
                                    if (evt.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();
                                        // If top, insert at this index (i.e., become this ID)
                                        // If bottom, insert after (i.e., become ID + 1)
                                        // Note: In Chapter loop, chapterGroup.Key is the ID.
                                        int targetChID = isTop ? chapterGroup.Key : chapterGroup.Key + 1;
                                        PerformChapterReorder(_currentDragData, epGroup.Key, targetChID);
                                        _currentDragData = null;
                                        Event.current.Use();
                                    }
                                }
                            }

                            // Visuals
                            if (evt.type == EventType.Repaint && isInChRect)
                            {
                                if (isTop)
                                    EditorGUI.DrawRect(new Rect(0, headerRect.y - 1, headerWidth, 2), Color.cyan);
                                else
                                    EditorGUI.DrawRect(new Rect(0, headerRect.yMax - 1, headerWidth, 2), Color.cyan);
                            }
                        }

                        // 4. Drop Logic (Section Auto-Append to Chapter)
                        // If dragging a Section and hovering Chapter header -> Append to End of Chapter
                        if (_currentDragData != null && _currentDragData.Type == DragType.Section)
                        {
                            // ... existing logic for section drop on header ...
                            if (headerRect.Contains(evt.mousePosition))
                            {
                                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                                {
                                    bool blocked = (evt.mousePosition.x > width - 50);
                                    DragAndDrop.visualMode = blocked ? DragAndDropVisualMode.None : DragAndDropVisualMode.Move;

                                    if (evt.type == EventType.DragUpdated && !blocked)
                                    {
                                        Event.current.Use(); // Just consume
                                    }

                                    if (evt.type == EventType.DragPerform && !blocked)
                                    {
                                        int maxSec = 0;
                                        if (chapterGroup.Any()) maxSec = chapterGroup.Max(u => u.SectionID);
                                        // Move Section
                                        PerformSectionReorder(_currentDragData, epGroup.Key, chapterGroup.Key, maxSec + 1); // Using Reorder method for move
                                        DragAndDrop.AcceptDrag();
                                        _currentDragData = null;
                                        Event.current.Use();
                                    }
                                }
                                // Visual
                                if (evt.type == EventType.Repaint && headerRect.Contains(evt.mousePosition) && !(evt.mousePosition.x > width - 50))
                                {
                                    EditorGUI.DrawRect(headerRect, new Color(1f, 1f, 0f, 0.2f));
                                }
                            }
                        }

                        // Buttons
                        Rect btnRectPlus = new Rect(headerRect.xMax - 42, headerRect.y, 20, headerRect.height);
                        Rect btnRectMinus = new Rect(headerRect.xMax - 20, headerRect.y, 20, headerRect.height);

                        if (GUI.Button(btnRectPlus, "+", EditorStyles.miniButtonLeft))
                        {
                            RequestCreateSection(epGroup.Key, chapterGroup.Key);
                        }
                        if (GUI.Button(btnRectMinus, "-", EditorStyles.miniButtonRight))
                        {
                            DeleteChapter(epGroup.Key, chapterGroup.Key);
                        }

                        // =========================================================

                        EditorGUI.indentLevel++;

                        var sections = chapterGroup.GroupBy(u => new
                        {
                            ID = u.SectionID,
                            NameKey = (u.SectionID == -1 ? u.SectionName : "")
                        }).OrderBy(g => g.Key.ID).ThenBy(g => g.Key.NameKey).ToList();

                        for (int i = 0; i < sections.Count; i++)
                        {
                            var sectionGroup = sections[i];
                            int secID = sectionGroup.Key.ID;
                            string secNameKey = sectionGroup.Key.NameKey;

                            if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                            var first = sectionGroup.FirstOrDefault();
                            string displayName = string.IsNullOrEmpty(first?.SectionName) ? (secID == -1 ? "Default" : secID.ToString()) : first.SectionName;

                            EditorGUILayout.BeginHorizontal();

                            GUILayout.Label("=", GUILayout.Width(20));
                            Rect handleRect = GUILayoutUtility.GetLastRect();

                            // Drag Start (Section)
                            if (_currentDragData == null && evt.type == EventType.MouseDrag && handleRect.Contains(evt.mousePosition))
                            {
                                _currentDragData = new SidebarDragData { Type = DragType.Section, Ep = epGroup.Key, Ch = chapterGroup.Key, Sec = secID };
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.SetGenericData("SectionDrag", _currentDragData);
                                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                                DragAndDrop.StartDrag($"Move Section {displayName}");
                                Event.current.Use();
                            }

                            if (GUILayout.Button($"{displayName} ({sectionGroup.Count()})", EditorStyles.miniButtonLeft))
                            {
                                string filterName = (secID == -1) ? secNameKey : null;
                                OnSelectSection?.Invoke(epGroup.Key, chapterGroup.Key, secID, filterName);
                                if (first != null) OnRequestPanTo?.Invoke(first.Position);
                            }

                            EditorGUILayout.EndHorizontal();

                            // --- SECTION DROP LOGIC ---
                            Rect rowRect = GUILayoutUtility.GetLastRect();
                            rowRect.x = 0;
                            rowRect.width = width;
                            float spacing = 1f;

                            Rect dropZoneRect = new Rect(0, rowRect.y + (rowRect.height * 0.5f), width, rowRect.height);
                            Rect topZoneRect = new Rect(0, rowRect.y, width, rowRect.height * 0.5f);

                            if (_currentDragData != null && _currentDragData.Type == DragType.Section)
                            {
                                bool isInDropZone = dropZoneRect.Contains(evt.mousePosition);
                                bool isInTopZone = (i == 0) && topZoneRect.Contains(evt.mousePosition);

                                if (isInDropZone || isInTopZone)
                                {
                                    if (evt.type == EventType.DragUpdated)
                                    {
                                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                        Event.current.Use();
                                    }
                                    if (evt.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();
                                        int targetIndex = isInTopZone ? 0 : i + 1;
                                        PerformSectionReorder(_currentDragData, epGroup.Key, chapterGroup.Key, targetIndex);
                                        _currentDragData = null;
                                        Event.current.Use();
                                    }
                                }

                                if (evt.type == EventType.Repaint)
                                {
                                    if (dropZoneRect.Contains(evt.mousePosition))
                                    {
                                        float lineY = rowRect.yMax + (spacing * 0.5f);
                                        EditorGUI.DrawRect(new Rect(0, lineY - 1, width, 2), Color.cyan);
                                    }
                                    else if (isInTopZone) // re-check for safety
                                    {
                                        EditorGUI.DrawRect(new Rect(0, rowRect.y - 1, width, 2), Color.cyan);
                                    }
                                }
                            }

                            // Context Menu
                            Rect btnRect = rowRect;
                            if (evt.type == EventType.MouseDown && evt.button == 1 && btnRect.Contains(evt.mousePosition))
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Delete Section"), false, () => DeleteSection(epGroup.Key, chapterGroup.Key, secID));
                                menu.ShowAsContext();
                                Event.current.Use();
                            }

                            GUILayout.Space(spacing);
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

            Rect divider = GUILayoutUtility.GetLastRect();
            divider.x += divider.width;
            divider.width = 1;
            EditorGUI.DrawRect(divider, Color.black);
        }

        // --- DATA & LOGIC ---

        private enum DragType { Section, Chapter }

        private class SidebarDragData
        {
            public DragType Type;
            public int Ep;
            public int Ch;
            public int Sec;
        }

        private void PerformSectionReorder(SidebarDragData dragData, int targetEp, int targetCh, int insertIndex)
        {
            if (dragData.Type != DragType.Section) return;

            // Logic logic logic... reuse existing logic block
            // NOTE: Copying previous logic but adapting variable names

            // Create list of target
            var targetList = Data.Conversations
                .Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh)
                .GroupBy(u => u.SectionID).OrderBy(g => g.Key).ToList();

            // Self-drop check
            if (dragData.Ep == targetEp && dragData.Ch == targetCh)
            {
                int currentIndex = targetList.FindIndex(g => g.Key == dragData.Sec);
                if (currentIndex == -1) return;
                if (currentIndex == insertIndex) return;
                if (currentIndex + 1 == insertIndex) return;

                // Adjust index if moving down
                if (insertIndex > currentIndex) insertIndex--;
            }

            var movingUnits = Data.Conversations
                .Where(u => u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch && u.SectionID == dragData.Sec)
                .ToList();

            // Remove logic handled by exclude-filter in construction? 
            // Reuse robust logic:

            // 2. target units excluding moving ones
            var targetChapterUnits = Data.Conversations
                .Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh)
                .Where(u => !(u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch && u.SectionID == dragData.Sec))
                .GroupBy(u => u.SectionID)
                .OrderBy(g => g.Key)
                .ToList();

            // Insert to list of lists context
            var reordered = new List<List<ConversationUnit>>();
            foreach (var g in targetChapterUnits) reordered.Add(g.ToList());

            // Clamp
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > reordered.Count) insertIndex = reordered.Count;

            // Update IDs of moving units
            foreach (var unit in movingUnits)
            {
                unit.EpisodeID = targetEp;
                unit.ChapterID = targetCh;
            }

            reordered.Insert(insertIndex, movingUnits);

            // Reassign SectionIDs
            int newSecID = 1;
            foreach (var g in reordered)
            {
                foreach (var unit in g) unit.SectionID = newSecID;
                newSecID++;
            }

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
            Debug.Log($"Moved Section {dragData.Sec} to {targetEp}/{targetCh} index {insertIndex}");
        }

        private void PerformChapterReorder(SidebarDragData dragData, int targetEp, int targetChID)
        {
            if (dragData.Type != DragType.Chapter) return;

            // 1. Get all units in the moving chapter
            var movingUnits = Data.Conversations
                .Where(u => u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch)
                .ToList();

            if (!movingUnits.Any()) return;

            // 2. Determine target ordering
            // Get all existing ChapterIDs in target Episode (excluding the moving one if same Ep)
            // But actually we are dealing with raw IDs in the target episode.

            // List of Chapters in Target Episode
            var targetChapters = Data.Conversations
                .Where(u => u.EpisodeID == targetEp)
                .Where(u => !(u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch)) // Exclude self if same episode
                .GroupBy(u => u.ChapterID)
                .OrderBy(g => g.Key)
                .ToList();

            // Map targetChID to list index
            // If dropping on Ch 2 (targetChID=2), we want to be at index where key was 2?
            // Actually targetChID is the "Desired ID".
            // Logic:
            // Iterate through sorted existing chapters.
            // Construct a new list of chapters.

            var newChapterOrder = new List<List<ConversationUnit>>();

            // Logic: targetChID implies insertion point.
            // If we drop on top of Ch 5, target is 5. We insert BEFORE 5.
            // If we drop bottom of Ch 5, target is 6. We insert AFTER 5.

            // Find insertion index in the filtered list
            int insertIndex = 0;
            bool found = false;
            for (int i = 0; i < targetChapters.Count; i++)
            {
                // If the current chapter has ID >= targetChID, we insert before it?
                // Visual logic: Top of 5 -> Target 5. List: 1, 2, 4, 5. Insert at index matching 5.
                // Bottom of 5 -> Target 6. List: 1, 2, 4, 5. Insert at index after 5.

                // Simpler: Compare keys.
                if (targetChapters[i].Key < targetChID)
                {
                    insertIndex = i + 1;
                }
            }
            // Logic tweak: If Self is Same Episode, we need to handle index shift logic similar to sections.
            if (dragData.Ep == targetEp)
            {
                // Visual drop logic gave us a TargetID based on VISUAL layout.
                // If I drag Ch 2 below Ch 3. Target is 4.
                // Existing: 1, 2, 3, 4.
                // Filtered: 1, 3, 4.
                // 3 < 4, Index = 2 (after 3). 
                // Insert at 2: 1, 3, [2], 4. -> Re-ID -> 1, 2, 3, 4. (Wait, 2 becomes 3, 3 becomes 2).
            }

            foreach (var g in targetChapters) newChapterOrder.Add(g.ToList());

            // Clamp
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > newChapterOrder.Count) insertIndex = newChapterOrder.Count;

            // Update Moving Units Data
            foreach (var unit in movingUnits)
            {
                unit.EpisodeID = targetEp;
                // ChapterID will be reassigned
            }

            newChapterOrder.Insert(insertIndex, movingUnits);

            // Reassign IDs
            int newID = 1;
            foreach (var chList in newChapterOrder)
            {
                foreach (var unit in chList) unit.ChapterID = newID;
                newID++;
            }

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
            Debug.Log($"Moved Chapter {dragData.Ch} to Ep {targetEp} as Ch {insertIndex + 1}");
        }

        // Helper Methods (Keep existing Create/Delete methods)
        private void CreateChapter(int epID)
        {
            int nextCh = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID))
            {
                nextCh = Data.Conversations.Where(u => u.EpisodeID == epID).Max(u => u.ChapterID) + 1;
            }
            int startSec = 1;
            string defaultName = GetUniqueSectionName(epID, nextCh);
            CreateSection(epID, nextCh, startSec, defaultName);
        }

        private void RequestCreateSection(int epID, int chapterID)
        {
            int nextSec = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID))
            {
                nextSec = Data.Conversations.Where(u => u.EpisodeID == epID && u.ChapterID == chapterID).Max(u => u.SectionID) + 1;
            }
            string defaultName = GetUniqueSectionName(epID, chapterID);
            CreateSection(epID, chapterID, nextSec, defaultName);
        }

        private string GetUniqueSectionName(int epID, int chapterID)
        {
            string baseName = "NewSection";
            int count = 1;
            string candidate = baseName;
            while (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID && u.SectionName == candidate))
            {
                candidate = $"{baseName}_{count++}";
            }
            return candidate;
        }

        private void CreateSection(int epID, int chapterID, int sectionID, string sectionName)
        {
            var newNode = new ConversationUnit
            {
                ID = $"{epID}_{chapterID}_{sectionID}_1",
                EpisodeID = epID,
                ChapterID = chapterID,
                SectionID = sectionID,
                SectionName = sectionName,
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
            int ch = 1;
            RequestCreateSection(newEp, ch);
        }

        private void DeleteSection(int ep, int chapter, int section)
        {
            if (EditorUtility.DisplayDialog("Delete Section", $"Are you sure you want to delete section '{section}' in '{ep}/{chapter}'?", "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section);
            }
        }

        private void DeleteChapter(int ep, int chapter)
        {
            if (EditorUtility.DisplayDialog("Delete Chapter", $"Are you sure you want to delete Chapter {chapter} in Episode {ep} and ALL its sections?", "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter);
            }
        }
    }
}
