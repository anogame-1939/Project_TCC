using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoDialogue.Editor
{
    /// <summary>
    /// UIToolkit-based sidebar for the Dialogue Graph, replacing the IMGUI DialogueGraphSidebar.
    /// Displays Episode > Chapter > Section hierarchy for navigation.
    /// </summary>
    public class DialogueGraphSidebarUT : VisualElement
    {
        public MasterDialogueData Data { get; private set; }

        // Events
        public Action<int, int, int, string> OnSelectSection;
        public Action<Vector2> OnRequestPanTo;

        private ToolbarSearchField _searchField;
        private ScrollView _scrollView;
        private string _searchFilter = "";
        private Dictionary<int, bool> _foldoutStates = new Dictionary<int, bool>();

        // --- Drag & Drop ---
        private bool _isDragging;
        private bool _dragStarted;
        private Vector2 _dragStartPos;
        private int _dragEp, _dragCh, _dragSec;
        private string _dragSectionName;
        private VisualElement _dragGhost;
        private VisualElement _dropIndicator;
        private const float DragThreshold = 5f;

        /// <summary>
        /// Information about a potential drop target in the sidebar.
        /// </summary>
        private struct DropTarget
        {
            public int EpisodeID;
            public int ChapterID;
            public int SectionID;  // -1 means "insert at end of chapter"
            public bool InsertBefore;
            public VisualElement Row;
        }
        private List<DropTarget> _dropTargets = new List<DropTarget>();

        public DialogueGraphSidebarUT(MasterDialogueData data)
        {
            Data = data;
            AddToClassList("dialogue-sidebar");

            // Header
            var header = new Label("Navigator");
            header.AddToClassList("sidebar-header");
            Add(header);

            // Search
            _searchField = new ToolbarSearchField();
            _searchField.AddToClassList("sidebar-search");
            _searchField.style.flexShrink = 1;
            _searchField.style.maxWidth = new StyleLength(new Length(100, LengthUnit.Percent));
            _searchField.style.width = new StyleLength(StyleKeyword.Auto);
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                RebuildTree();
            });
            Add(_searchField);

            // Scroll
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.AddToClassList("sidebar-scroll");
            Add(_scrollView);

            // Add Episode button
            var addEpBtn = new Button(() => CreateEpisode()) { text = "Add Episode" };
            addEpBtn.AddToClassList("sidebar-add-episode-btn");
            Add(addEpBtn);

            RebuildTree();
        }

        public void RebuildTree()
        {
            _scrollView.Clear();
            _dropTargets.Clear();

            if (Data == null || Data.Conversations == null || Data.Conversations.Count == 0)
            {
                _scrollView.Add(new Label("No data") { style = { color = new StyleColor(Color.gray), unityTextAlign = TextAnchor.MiddleCenter } });
                return;
            }

            var episodes = Data.Conversations.GroupBy(u => u.EpisodeID).OrderBy(g => g.Key);

            foreach (var epGroup in episodes)
            {
                int epKey = epGroup.Key;
                string epStr = epKey == 0 ? "Default" : epKey.ToString();
                bool allowEp = string.IsNullOrEmpty(_searchFilter) || epStr.Contains(_searchFilter);

                // Get saved foldout state (default: open)
                if (!_foldoutStates.ContainsKey(epKey))
                    _foldoutStates[epKey] = true;

                string foldoutLabel = epKey == 0 ? "Chapter: Default" : $"Episode: {epStr}";
                var epFoldout = new Foldout { text = foldoutLabel, value = _foldoutStates[epKey] };
                epFoldout.AddToClassList("episode-foldout");

                // Save foldout state on toggle
                int capturedKey = epKey;
                epFoldout.RegisterValueChangedCallback(evt =>
                {
                    _foldoutStates[capturedKey] = evt.newValue;
                });

                if (epKey != 0)
                {
                    // + button for add chapter
                    var epHeader = epFoldout.Q<Toggle>();
                    if (epHeader != null)
                    {
                        var addChBtn = new Button(() => CreateChapter(epKey)) { text = "+" };
                        addChBtn.AddToClassList("chapter-btn");
                        addChBtn.style.position = Position.Absolute;
                        addChBtn.style.right = 6;
                        addChBtn.style.top = 0;
                        epHeader.Add(addChBtn);
                    }
                }

                BuildChaptersIntoContainer(epFoldout, epGroup, allowEp);
                _scrollView.Add(epFoldout);
            }
        }

        private void BuildChapters(IGrouping<int, ConversationUnit> epGroup, bool allowEp)
        {
            var container = new VisualElement();
            BuildChaptersIntoContainer(container, epGroup, allowEp);
            _scrollView.Add(container);
        }

        private void BuildChaptersIntoContainer(VisualElement container, IGrouping<int, ConversationUnit> epGroup, bool allowEp)
        {
            var chapters = epGroup.GroupBy(u => u.ChapterID).OrderBy(g => g.Key);

            foreach (var chapterGroup in chapters)
            {
                string chStr = chapterGroup.Key == 0 ? "Default" : chapterGroup.Key.ToString();
                bool allowChapter = allowEp || chStr.Contains(_searchFilter);

                // Chapter row
                var chRow = new VisualElement();
                chRow.AddToClassList("chapter-row");

                var chLabel = new Label($"Chapter: {chStr}");
                chLabel.AddToClassList("chapter-label");
                chRow.Add(chLabel);

                var addSecBtn = new Button(() => RequestCreateSection(epGroup.Key, chapterGroup.Key)) { text = "+" };
                addSecBtn.AddToClassList("chapter-btn");
                chRow.Add(addSecBtn);

                var delChBtn = new Button(() => DeleteChapter(epGroup.Key, chapterGroup.Key)) { text = "-" };
                delChBtn.AddToClassList("chapter-btn");
                chRow.Add(delChBtn);

                container.Add(chRow);

                // Sections
                var sections = chapterGroup.GroupBy(u => new
                {
                    ID = u.SectionID,
                    NameKey = (u.SectionID == 0 ? u.SectionName : "")
                }).OrderBy(g => g.Key.ID).ThenBy(g => g.Key.NameKey).ToList();

                foreach (var sectionGroup in sections)
                {
                    int secID = sectionGroup.Key.ID;
                    string secNameKey = sectionGroup.Key.NameKey;

                    if (secID == 0 && (string.IsNullOrEmpty(secNameKey) || secNameKey == "Default")) continue;

                    if (!allowChapter && !sectionGroup.Any(u => u.ID != null && u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                    var first = sectionGroup.FirstOrDefault();
                    string displayName = string.IsNullOrEmpty(first?.SectionName) ? (secID == 0 ? "Default" : secID.ToString()) : first.SectionName;

                    var secRow = new VisualElement();
                    secRow.AddToClassList("section-row");

                    // Capture for closure
                    int capturedEp = epGroup.Key;
                    int capturedCh = chapterGroup.Key;
                    int capturedSec = secID;
                    string capturedName = secNameKey;

                    // Section name label (left, ellipsis overflow)
                    var secNameLabel = new Label(displayName);
                    secNameLabel.AddToClassList("section-name");
                    secNameLabel.pickingMode = PickingMode.Ignore;

                    // Section count label (right-aligned)
                    var secCountLabel = new Label(sectionGroup.Count().ToString());
                    secCountLabel.AddToClassList("section-count");
                    secCountLabel.pickingMode = PickingMode.Ignore;

                    // --- All interactions (click, double-click, DD, context menu) handled via SecRow ---
                    RegisterDragEvents(secRow, secNameLabel, secCountLabel, capturedEp, capturedCh, capturedSec, capturedName, displayName, first);

                    // Right-click context menu on secRow
                    secRow.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
                    {
                        evt.menu.AppendAction("Rename", action =>
                        {
                            StartInlineRename(secNameLabel, capturedEp, capturedCh, capturedSec, displayName);
                        });
                        evt.menu.AppendAction("Delete Section", action =>
                        {
                            DeleteSection(capturedEp, capturedCh, capturedSec);
                        });
                    });

                    // Register as drop target
                    _dropTargets.Add(new DropTarget
                    {
                        EpisodeID = capturedEp,
                        ChapterID = capturedCh,
                        SectionID = capturedSec,
                        InsertBefore = false,
                        Row = secRow
                    });

                    secRow.Add(secNameLabel);
                    secRow.Add(secCountLabel);

                    // Delete button for section
                    var secDelBtn = new Button(() =>
                    {
                        DeleteSection(capturedEp, capturedCh, capturedSec);
                    })
                    { text = "×" };
                    secDelBtn.AddToClassList("section-delete-btn");
                    secRow.Add(secDelBtn);

                    container.Add(secRow);
                }
            }
        }

        // --- Drag & Drop ---

        private void RegisterDragEvents(VisualElement secRow, Label secNameLabel, Label secCountLabel, int ep, int ch, int sec, string nameKey, string displayName, ConversationUnit first)
        {
            // PointerDown: record drag start + detect double-click
            secRow.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;

                // Double-click → rename
                if (evt.clickCount == 2)
                {
                    Debug.Log("[Sidebar] Double-click detected -> StartInlineRename");
                    evt.StopImmediatePropagation();
                    _dragStarted = false;
                    _isDragging = false;
                    StartInlineRename(secNameLabel, ep, ch, sec, displayName);
                    return;
                }

                if (_isDragging) return;

                // Record for potential drag
                _dragStarted = true;
                _dragStartPos = evt.position;
                _dragEp = ep;
                _dragCh = ch;
                _dragSec = sec;
                _dragSectionName = displayName;

                // Capture immediately so we get all PointerMove/PointerUp
                secRow.CapturePointer(evt.pointerId);
            }, TrickleDown.TrickleDown);

            // PointerMove: detect drag threshold
            secRow.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_dragStarted && !_isDragging) return;

                var delta = (Vector2)evt.position - _dragStartPos;

                if (!_isDragging)
                {
                    if (delta.magnitude < DragThreshold) return;

                    // Threshold exceeded: begin drag
                    Debug.Log($"[Sidebar] Drag threshold exceeded: delta={delta.magnitude:F1}");
                    _isDragging = true;
                    BeginDrag(secRow);
                }

                if (_isDragging)
                {
                    UpdateDrag(evt.position);
                }
            }, TrickleDown.TrickleDown);

            // PointerUp: handle drop or click
            secRow.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (secRow.HasPointerCapture(evt.pointerId))
                    secRow.ReleasePointer(evt.pointerId);

                if (_isDragging)
                {
                    EndDrag(evt.position);
                }
                else if (_dragStarted)
                {
                    // No drag occurred → treat as click (select section)
                    string filterName = (sec == 0) ? nameKey : null;
                    OnSelectSection?.Invoke(ep, ch, sec, filterName);
                    if (first != null)
                    {
                        OnRequestPanTo?.Invoke(first.Position);
                    }
                }

                _dragStarted = false;
                _isDragging = false;
            }, TrickleDown.TrickleDown);

            secRow.RegisterCallback<PointerCaptureOutEvent>(evt =>
            {
                if (evt.target != secRow) return;
                if (_isDragging)
                {
                    CancelDrag();
                }
                _dragStarted = false;
                _isDragging = false;
            });

            secRow.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && _isDragging)
                {
                    CancelDrag();
                    _dragStarted = false;
                    _isDragging = false;
                    evt.StopPropagation();
                }
            });
        }

        private void BeginDrag(VisualElement sourceRow)
        {
            Debug.Log($"[Sidebar] BeginDrag: section='{_dragSectionName}' ({_dragEp}/{_dragCh}/{_dragSec}), dropTargets={_dropTargets.Count}");

            // Create ghost
            _dragGhost = new VisualElement();
            _dragGhost.AddToClassList("drag-ghost");
            var ghostLabel = new Label(_dragSectionName);
            ghostLabel.AddToClassList("drag-ghost-label");
            _dragGhost.Add(ghostLabel);

            // Position ghost at initial place
            var rootPanel = panel.visualTree;
            rootPanel.Add(_dragGhost);
            Debug.Log($"[Sidebar] Ghost added to panel. rootPanel children={rootPanel.childCount}");

            // Create drop indicator line
            _dropIndicator = new VisualElement();
            _dropIndicator.AddToClassList("drop-indicator");
            _dropIndicator.style.display = DisplayStyle.None;
            rootPanel.Add(_dropIndicator);

            // Highlight source row
            sourceRow.AddToClassList("section-row--dragging");
        }

        private void UpdateDrag(Vector2 pointerPos)
        {
            if (_dragGhost == null) return;

            // Move ghost to follow pointer
            _dragGhost.style.left = pointerPos.x + 10;
            _dragGhost.style.top = pointerPos.y - 10;

            // Find drop target
            bool foundTarget = false;
            foreach (var target in _dropTargets)
            {
                if (target.Row == null || target.Row.panel == null) continue;
                // Skip self
                if (target.EpisodeID == _dragEp && target.ChapterID == _dragCh && target.SectionID == _dragSec) continue;

                var rowWorldBound = target.Row.worldBound;
                if (rowWorldBound.Contains(pointerPos))
                {
                    foundTarget = true;
                    Debug.Log($"[Sidebar] UpdateDrag: found target sec={target.SectionID} at worldBound={rowWorldBound}, pointerPos={pointerPos}");

                    // Determine insert position: top half = before, bottom half = after
                    float midY = rowWorldBound.y + rowWorldBound.height * 0.5f;
                    bool insertBefore = pointerPos.y < midY;
                    float indicatorY = insertBefore ? rowWorldBound.yMin : rowWorldBound.yMax;

                    _dropIndicator.style.display = DisplayStyle.Flex;
                    _dropIndicator.style.left = rowWorldBound.x;
                    _dropIndicator.style.top = indicatorY - 1;
                    _dropIndicator.style.width = rowWorldBound.width;

                    // Store intent in userData
                    _dropIndicator.userData = new DropTarget
                    {
                        EpisodeID = target.EpisodeID,
                        ChapterID = target.ChapterID,
                        SectionID = target.SectionID,
                        InsertBefore = insertBefore,
                        Row = target.Row
                    };

                    // Highlight
                    ClearDropHighlights();
                    target.Row.AddToClassList("section-row--drag-over");
                    break;
                }
            }

            if (!foundTarget)
            {
                _dropIndicator.style.display = DisplayStyle.None;
                ClearDropHighlights();
            }
        }

        private void EndDrag(Vector2 pointerPos)
        {
            bool hasIndicator = _dropIndicator != null;
            bool isDropTarget = hasIndicator && _dropIndicator.userData is DropTarget;
            bool isVisible = hasIndicator && _dropIndicator.resolvedStyle.display == DisplayStyle.Flex;
            Debug.Log($"[Sidebar] EndDrag: hasIndicator={hasIndicator}, isDropTarget={isDropTarget}, isVisible={isVisible}");

            if (hasIndicator && _dropIndicator.userData is DropTarget dropTarget && isVisible)
            {
                Debug.Log($"[Sidebar] PerformSectionMove: from ({_dragEp}/{_dragCh}/{_dragSec}) to ({dropTarget.EpisodeID}/{dropTarget.ChapterID}/{dropTarget.SectionID}), insertBefore={dropTarget.InsertBefore}");
                PerformSectionMove(dropTarget);
            }
            else
            {
                Debug.Log("[Sidebar] EndDrag: no valid drop target, canceling");
            }

            CleanupDrag();
        }

        private void CancelDrag()
        {
            CleanupDrag();
        }

        private void CleanupDrag()
        {
            if (_dragGhost != null)
            {
                _dragGhost.RemoveFromHierarchy();
                _dragGhost = null;
            }
            if (_dropIndicator != null)
            {
                _dropIndicator.RemoveFromHierarchy();
                _dropIndicator = null;
            }
            ClearDropHighlights();
            ClearDragSourceHighlight();
        }

        private void ClearDropHighlights()
        {
            _scrollView?.Query(className: "section-row--drag-over").ForEach(e => e.RemoveFromClassList("section-row--drag-over"));
        }

        private void ClearDragSourceHighlight()
        {
            _scrollView?.Query(className: "section-row--dragging").ForEach(e => e.RemoveFromClassList("section-row--dragging"));
        }

        private void PerformSectionMove(DropTarget target)
        {
            // Get all units belonging to the dragged section
            var draggedUnits = Data.Conversations.Where(u =>
                u.EpisodeID == _dragEp && u.ChapterID == _dragCh && u.SectionID == _dragSec).ToList();

            if (draggedUnits.Count == 0) return;

            // Determine new SectionID
            int newSectionID;
            if (target.InsertBefore)
            {
                newSectionID = target.SectionID;
                // Shift existing sections at and after this position
                var toShift = Data.Conversations.Where(u =>
                    u.EpisodeID == target.EpisodeID &&
                    u.ChapterID == target.ChapterID &&
                    u.SectionID >= newSectionID &&
                    !(u.EpisodeID == _dragEp && u.ChapterID == _dragCh && u.SectionID == _dragSec)
                ).ToList();
                foreach (var u in toShift)
                    u.SectionID++;
            }
            else
            {
                newSectionID = target.SectionID + 1;
                // Shift existing sections after target
                var toShift = Data.Conversations.Where(u =>
                    u.EpisodeID == target.EpisodeID &&
                    u.ChapterID == target.ChapterID &&
                    u.SectionID >= newSectionID &&
                    !(u.EpisodeID == _dragEp && u.ChapterID == _dragCh && u.SectionID == _dragSec)
                ).ToList();
                foreach (var u in toShift)
                    u.SectionID++;
            }

            // Move the dragged units
            foreach (var u in draggedUnits)
            {
                u.EpisodeID = target.EpisodeID;
                u.ChapterID = target.ChapterID;
                u.SectionID = newSectionID;
            }

            // Compact section IDs in source chapter (if different from target)
            if (_dragEp != target.EpisodeID || _dragCh != target.ChapterID)
            {
                CompactSectionIDs(_dragEp, _dragCh);
            }
            // Also compact target chapter
            CompactSectionIDs(target.EpisodeID, target.ChapterID);

            EditorUtility.SetDirty(Data);
            RebuildTree();
        }

        private void CompactSectionIDs(int ep, int ch)
        {
            var sections = Data.Conversations
                .Where(u => u.EpisodeID == ep && u.ChapterID == ch)
                .GroupBy(u => u.SectionID)
                .OrderBy(g => g.Key)
                .ToList();

            int newID = 1;
            foreach (var group in sections)
            {
                foreach (var u in group)
                    u.SectionID = newID;
                newID++;
            }
        }

        // --- Inline Rename ---
        private void StartInlineRename(Label sourceNameLabel, int ep, int ch, int sec, string currentName)
        {
            var parent = sourceNameLabel.parent;
            var idx = parent.IndexOf(sourceNameLabel);

            var textField = new TextField();
            textField.value = currentName;
            textField.AddToClassList("section-name");
            textField.style.flexGrow = 1;

            parent.Insert(idx, textField);
            sourceNameLabel.style.display = DisplayStyle.None;

            textField.Focus();
            textField.SelectAll();

            Action commitRename = () =>
            {
                string newName = textField.value;
                var units = Data.Conversations.Where(u => u.EpisodeID == ep && u.ChapterID == ch && u.SectionID == sec).ToList();
                foreach (var u in units)
                {
                    u.SectionName = newName;
                }
                EditorUtility.SetDirty(Data);

                parent.Remove(textField);
                sourceNameLabel.text = newName;
                sourceNameLabel.style.display = DisplayStyle.Flex;
            };

            Action cancelRename = () =>
            {
                parent.Remove(textField);
                sourceNameLabel.style.display = DisplayStyle.Flex;
            };

            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    commitRename();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    cancelRename();
                    evt.StopPropagation();
                }
            });

            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                // Commit on focus loss
                if (textField.parent != null)
                {
                    commitRename();
                }
            });
        }

        // --- Data Operations ---

        private void CreateEpisode()
        {
            int newEp = 1;
            if (Data.Conversations.Any())
            {
                newEp = Data.Conversations.Max(u => u.EpisodeID) + 1;
            }
            RequestCreateSection(newEp, 1);
        }

        private void CreateChapter(int epID)
        {
            int nextCh = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID))
            {
                nextCh = Data.Conversations.Where(u => u.EpisodeID == epID).Max(u => u.ChapterID) + 1;
            }
            string defaultName = GetUniqueSectionName(epID, nextCh);
            CreateSection(epID, nextCh, 1, defaultName);
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
            EditorUtility.SetDirty(Data);
            RebuildTree();
        }

        private void DeleteSection(int ep, int chapter, int section)
        {
            if (EditorUtility.DisplayDialog("Delete Section",
                $"Are you sure you want to delete section '{section}' in '{ep}/{chapter}'?",
                "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section);
                EditorUtility.SetDirty(Data);
                RebuildTree();
            }
        }

        private void DeleteChapter(int ep, int chapter)
        {
            if (EditorUtility.DisplayDialog("Delete Chapter",
                $"Are you sure you want to delete Chapter {chapter} in Episode {ep} and ALL its sections?",
                "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter);
                EditorUtility.SetDirty(Data);
                RebuildTree();
            }
        }
    }
}
