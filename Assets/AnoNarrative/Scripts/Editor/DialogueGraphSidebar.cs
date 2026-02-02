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

        // Rename State
        private class RenameState
        {
            public int Ep;
            public int Ch;
            public int Sec;
            public string CurrentText;
            public bool IsActive;
        }
        private RenameState _renameState = new RenameState();

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
                // Cancel Rename on Escape
                if (_renameState.IsActive && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
                {
                    CancelRename();
                    evt.Use();
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
                    // Hide Episode Header if Default (ID -1)
                    if (epGroup.Key != -1)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Episode: {epStr}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            CreateChapter(epGroup.Key);
                        }
                        EditorGUILayout.EndHorizontal();

                        // Episode Drop Zone [...]
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
                                        int targetCh = 1;
                                        if (epGroup.Any()) targetCh = epGroup.Max(u => u.ChapterID) + 1;
                                        PerformChapterReorder(_currentDragData, epGroup.Key, targetCh);
                                        _currentDragData = null;
                                        Event.current.Use();
                                    }
                                }
                            }
                            if (evt.type == EventType.Repaint && epRect.Contains(evt.mousePosition))
                            {
                                EditorGUI.DrawRect(epRect, new Color(0, 1, 1, 0.2f));
                            }
                        }

                        EditorGUI.indentLevel++;
                    }

                    var chapters = epGroup.GroupBy(u => u.ChapterID).OrderBy(g => g.Key);

                    foreach (var chapterGroup in chapters)
                    {
                        string chStr = chapterGroup.Key == -1 ? "Default" : chapterGroup.Key.ToString();
                        bool allowChapter = allowEp || chStr.Contains(_searchFilter);

                        Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        float headerWidth = width;

                        Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - 50, headerRect.height);
                        EditorGUI.LabelField(labelRect, $"{chStr}", EditorStyles.miniBoldLabel);

                        // Drag Start (Chapter) [...]
                        if (_currentDragData == null && evt.type == EventType.MouseDrag && labelRect.Contains(evt.mousePosition))
                        {
                            // Debug.Log($"Drag Start Chapter: {chStr}");
                            _currentDragData = new SidebarDragData { Type = DragType.Chapter, Ep = epGroup.Key, Ch = chapterGroup.Key };
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.SetGenericData("ChapterDrag", _currentDragData);
                            DragAndDrop.objectReferences = new UnityEngine.Object[0];
                            DragAndDrop.StartDrag($"Move Chapter {chStr}");
                            Event.current.Use();
                        }

                        // Drop Zone Logic (Chapter Reorder) [...]
                        if (_currentDragData != null && _currentDragData.Type == DragType.Chapter)
                        {
                            Rect chDropRect = new Rect(0, headerRect.y, headerWidth, headerRect.height);
                            Rect chTopZone = new Rect(0, headerRect.y, headerWidth, headerRect.height * 0.5f);

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
                                        int targetChID = isTop ? chapterGroup.Key : chapterGroup.Key + 1;
                                        PerformChapterReorder(_currentDragData, epGroup.Key, targetChID);
                                        _currentDragData = null;
                                        Event.current.Use();
                                    }
                                }
                            }
                            if (evt.type == EventType.Repaint && isInChRect)
                            {
                                if (isTop) EditorGUI.DrawRect(new Rect(0, headerRect.y - 1, headerWidth, 2), Color.cyan);
                                else EditorGUI.DrawRect(new Rect(0, headerRect.yMax - 1, headerWidth, 2), Color.cyan);
                            }
                        }

                        // Drop Logic (Section Append) [...]
                        if (_currentDragData != null && _currentDragData.Type == DragType.Section)
                        {
                            if (headerRect.Contains(evt.mousePosition))
                            {
                                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                                {
                                    bool blocked = (evt.mousePosition.x > width - 50);
                                    DragAndDrop.visualMode = blocked ? DragAndDropVisualMode.None : DragAndDropVisualMode.Move;
                                    if (evt.type == EventType.DragUpdated && !blocked) Event.current.Use();
                                    if (evt.type == EventType.DragPerform && !blocked)
                                    {
                                        int maxSec = 0;
                                        if (chapterGroup.Any()) maxSec = chapterGroup.Max(u => u.SectionID);
                                        PerformSectionReorder(_currentDragData, epGroup.Key, chapterGroup.Key, maxSec + 1);
                                        DragAndDrop.AcceptDrag();
                                        _currentDragData = null;
                                        Event.current.Use();
                                    }
                                }
                                if (evt.type == EventType.Repaint && headerRect.Contains(evt.mousePosition) && !(evt.mousePosition.x > width - 50))
                                {
                                    EditorGUI.DrawRect(headerRect, new Color(1f, 1f, 0f, 0.2f));
                                }
                            }
                        }

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

                            // Skip Default Section (-1) ONLY if it has no name or explicitly "Default"
                            // Many valid sections have ID -1 but distinct names.
                            if (secID == -1 && (string.IsNullOrEmpty(secNameKey) || secNameKey == "Default")) continue;

                            if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                            var first = sectionGroup.FirstOrDefault();
                            string displayName = string.IsNullOrEmpty(first?.SectionName) ? (secID == -1 ? "Default" : secID.ToString()) : first.SectionName;

                            EditorGUILayout.BeginHorizontal();

                            GUILayout.Label("=", GUILayout.Width(20));
                            Rect handleRect = GUILayoutUtility.GetLastRect();

                            // Drag Start (Section)
                            if (_currentDragData == null && evt.type == EventType.MouseDrag && handleRect.Contains(evt.mousePosition))
                            {
                                // Important: Pass displayName (SecName) to distinguish "Default" sections with same ID -1
                                _currentDragData = new SidebarDragData { Type = DragType.Section, Ep = epGroup.Key, Ch = chapterGroup.Key, Sec = secID, SecName = displayName };
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.SetGenericData("SectionDrag", _currentDragData);
                                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                                DragAndDrop.StartDrag($"Move Section {displayName}");
                                Event.current.Use();
                            }

                            // --- RENAME LOGIC START ---
                            bool isRenamingThis = _renameState.IsActive &&
                                                  _renameState.Ep == epGroup.Key &&
                                                  _renameState.Ch == chapterGroup.Key &&
                                                  _renameState.Sec == secID;

                            if (isRenamingThis)
                            {
                                // Rename Field
                                GUI.SetNextControlName("RenameField");
                                _renameState.CurrentText = EditorGUILayout.TextField(_renameState.CurrentText, EditorStyles.miniTextField);

                                // Focus logic
                                if (GUI.GetNameOfFocusedControl() != "RenameField")
                                {
                                    EditorGUI.FocusTextInControl("RenameField");
                                }

                                // Handle Enter to Commit
                                if (evt.isKey && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                                {
                                    CommitRename();
                                    evt.Use();
                                }
                            }
                            else
                            {
                                // Normal Button
                                // Double click check logic: 
                                // We can't just use evt.clickCount because button eats the event.
                                // But if we use GUILayout.Button, it returns true on click.
                                // We need to detect double click on the specific rect... 
                                // Easier way: Custom button logic or get last rect.


                                // Manual Rect Layout to capture events properly
                                // Use GetRect to reserve space within the horizontal layout
                                Rect btnRect = GUILayoutUtility.GetRect(new GUIContent($"{displayName} ({sectionGroup.Count()})"), EditorStyles.miniButtonLeft);

                                // 1. Right Click (Context Menu) - Explicit Check
                                if (evt.type == EventType.MouseDown && evt.button == 1 && btnRect.Contains(evt.mousePosition))
                                {
                                    GenericMenu menu = new GenericMenu();
                                    menu.AddItem(new GUIContent("Rename"), false, () => StartRename(epGroup.Key, chapterGroup.Key, secID, displayName));
                                    menu.AddItem(new GUIContent("Delete Section"), false, () => DeleteSection(epGroup.Key, chapterGroup.Key, secID));
                                    menu.ShowAsContext();
                                    evt.Use();
                                }

                                // 2. Double Click (Rename) - Explicit Check
                                if (evt.type == EventType.MouseDown && evt.clickCount == 2 && btnRect.Contains(evt.mousePosition) && evt.button == 0)
                                {
                                    StartRename(epGroup.Key, chapterGroup.Key, secID, displayName);
                                    evt.Use();
                                }

                                // 3. Left Click (Select) - Draw Button manually
                                if (GUI.Button(btnRect, $"{displayName} ({sectionGroup.Count()})", EditorStyles.miniButtonLeft))
                                {
                                    string filterName = (secID == -1) ? secNameKey : null;
                                    OnSelectSection?.Invoke(epGroup.Key, chapterGroup.Key, secID, filterName);
                                    if (first != null) OnRequestPanTo?.Invoke(first.Position);
                                }


                            }
                            // --- RENAME LOGIC END ---

                            EditorGUILayout.EndHorizontal();

                            // --- SECTION DROP LOGIC [...]
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
                                    else if (isInTopZone)
                                    {
                                        EditorGUI.DrawRect(new Rect(0, rowRect.y - 1, width, 2), Color.cyan);
                                    }
                                }
                            }

                            // Context Menu
                            Rect ctxBtnRect = rowRect;
                            if (evt.type == EventType.MouseDown && evt.button == 1 && ctxBtnRect.Contains(evt.mousePosition))
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Rename"), false, () => StartRename(epGroup.Key, chapterGroup.Key, secID, displayName));
                                menu.AddItem(new GUIContent("Delete Section"), false, () => DeleteSection(epGroup.Key, chapterGroup.Key, secID));
                                menu.ShowAsContext();
                                Event.current.Use();
                            }

                            GUILayout.Space(spacing);
                        }
                        EditorGUI.indentLevel--;
                    }
                    if (epGroup.Key != -1) EditorGUI.indentLevel--;
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

        private void StartRename(int ep, int ch, int sec, string currentName)
        {
            _renameState.Ep = ep;
            _renameState.Ch = ch;
            _renameState.Sec = sec;
            _renameState.CurrentText = currentName;
            _renameState.IsActive = true;
        }

        private void CommitRename()
        {
            if (!_renameState.IsActive) return;

            // Apply new name to all units in the section
            var units = Data.Conversations.Where(u => u.EpisodeID == _renameState.Ep && u.ChapterID == _renameState.Ch && u.SectionID == _renameState.Sec).ToList();

            foreach (var u in units)
            {
                u.SectionName = _renameState.CurrentText;
            }

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();

            CancelRename();
        }

        private void CancelRename()
        {
            _renameState.IsActive = false;
            GUI.FocusControl(null);
        }

        private enum DragType { Section, Chapter }

        private class SidebarDragData
        {
            public DragType Type;
            public int Ep;
            public int Ch;
            public int Sec;
            public string SecName;
        }

        // ... [Rest of Reorder Logic same as before] ...
        private void PerformSectionReorder(SidebarDragData dragData, int targetEp, int targetCh, int insertIndex)
        {
            if (dragData.Type != DragType.Section) return;
            var targetList = Data.Conversations.Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh).GroupBy(u => u.SectionID).OrderBy(g => g.Key).ToList();
            if (dragData.Ep == targetEp && dragData.Ch == targetCh)
            {
                int currentIndex = targetList.FindIndex(g => g.Key == dragData.Sec);
                if (currentIndex == -1) return;
                if (currentIndex == insertIndex) return;
                if (currentIndex + 1 == insertIndex) return;
                if (insertIndex > currentIndex) insertIndex--;
            }

            // Fix: Filter by Name as well if ID is -1, to avoid grabbing ALL default sections
            // If Sec != -1, ID is unique enough. If Sec == -1, we MUST key off SectionName.
            var movingUnits = Data.Conversations.Where(u =>
                u.EpisodeID == dragData.Ep &&
                u.ChapterID == dragData.Ch &&
                u.SectionID == dragData.Sec &&
                (dragData.Sec != -1 || u.SectionName == dragData.SecName)
            ).ToList();

            // When gathering "Others" in target, ensure we exclude the ones we just picked up
            // Note: We can reuse movingUnits reference check or repeat the logic. Ref check is safer.
            var targetChapterUnits = Data.Conversations.Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh)
                .Where(u => !movingUnits.Contains(u))
                .GroupBy(u => u.SectionID).OrderBy(g => g.Key).ToList();

            var reordered = new List<List<ConversationUnit>>();
            foreach (var g in targetChapterUnits) reordered.Add(g.ToList());

            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > reordered.Count) insertIndex = reordered.Count;

            foreach (var unit in movingUnits) { unit.EpisodeID = targetEp; unit.ChapterID = targetCh; }
            reordered.Insert(insertIndex, movingUnits);

            int newSecID = 1;
            foreach (var g in reordered) { foreach (var unit in g) unit.SectionID = newSecID; newSecID++; }
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
        }

        private void PerformChapterReorder(SidebarDragData dragData, int targetEp, int targetChID)
        {
            if (dragData.Type != DragType.Chapter) return;
            var movingUnits = Data.Conversations.Where(u => u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch).ToList();
            if (!movingUnits.Any()) return;
            var targetChapters = Data.Conversations.Where(u => u.EpisodeID == targetEp).Where(u => !(u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch)).GroupBy(u => u.ChapterID).OrderBy(g => g.Key).ToList();
            var newChapterOrder = new List<List<ConversationUnit>>();
            int insertIndex = 0;
            for (int i = 0; i < targetChapters.Count; i++) { if (targetChapters[i].Key < targetChID) { insertIndex = i + 1; } }
            foreach (var g in targetChapters) newChapterOrder.Add(g.ToList());
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > newChapterOrder.Count) insertIndex = newChapterOrder.Count;
            foreach (var unit in movingUnits) { unit.EpisodeID = targetEp; }
            newChapterOrder.Insert(insertIndex, movingUnits);
            int newID = 1;
            foreach (var chList in newChapterOrder) { foreach (var unit in chList) unit.ChapterID = newID; newID++; }
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
        }

        private void CreateChapter(int epID)
        {
            int nextCh = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID)) nextCh = Data.Conversations.Where(u => u.EpisodeID == epID).Max(u => u.ChapterID) + 1;
            int startSec = 1;
            string defaultName = GetUniqueSectionName(epID, nextCh);
            CreateSection(epID, nextCh, startSec, defaultName);
        }

        private void RequestCreateSection(int epID, int chapterID)
        {
            int nextSec = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID)) nextSec = Data.Conversations.Where(u => u.EpisodeID == epID && u.ChapterID == chapterID).Max(u => u.SectionID) + 1;
            string defaultName = GetUniqueSectionName(epID, chapterID);
            CreateSection(epID, chapterID, nextSec, defaultName);
        }

        private string GetUniqueSectionName(int epID, int chapterID)
        {
            string baseName = "NewSection";
            int count = 1;
            string candidate = baseName;
            while (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID && u.SectionName == candidate)) candidate = $"{baseName}_{count++}";
            return candidate;
        }

        private void CreateSection(int epID, int chapterID, int sectionID, string sectionName)
        {
            var newNode = new ConversationUnit { ID = $"{epID}_{chapterID}_{sectionID}_1", EpisodeID = epID, ChapterID = chapterID, SectionID = sectionID, SectionName = sectionName, SpeakerName = "New Speaker", BodyText = "Start", Position = new Vector2(100, 100) };
            Data.Conversations.Add(newNode);
        }

        private void CreateEpisode() { int newEp = 1; if (Data.Conversations.Any()) newEp = Data.Conversations.Max(u => u.EpisodeID) + 1; int ch = 1; RequestCreateSection(newEp, ch); }

        private void DeleteSection(int ep, int chapter, int section) { if (EditorUtility.DisplayDialog("Delete Section", $"Are you sure you want to delete section '{section}' in '{ep}/{chapter}'?", "Yes", "No")) Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section); }

        private void DeleteChapter(int ep, int chapter) { if (EditorUtility.DisplayDialog("Delete Chapter", $"Are you sure you want to delete Chapter {chapter} in Episode {ep} and ALL its sections?", "Yes", "No")) Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter); }
    }
}
