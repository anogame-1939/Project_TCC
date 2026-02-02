using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using AnoGame.AnoNarrative;

namespace AnoGame.AnoNarrative.Editor
{
    public class DialogueGraphCanvas
    {
        public MasterDialogueData Data;
        public DialogueGraphState State; // New State Container

        private EditorWindow _host;

        private const float NodeWidth = 255f;
        private const float NodeHeight = 92f;

        // Caching
        private Dictionary<string, Vector2> _posCache = new Dictionary<string, Vector2>();

        // Filter (int.MinValue means All)
        public int FilterEpisode = int.MinValue;
        public int FilterChapter = int.MinValue;
        public int FilterSection = int.MinValue;
        public string FilterSectionName = null; // Optional additional filter

        // Actions
        private List<ConversationUnit> _nodesToDelete = new List<ConversationUnit>();

        public DialogueGraphCanvas(MasterDialogueData data, EditorWindow host)
        {
            Data = data;
            _host = host;
            State = new DialogueGraphState(); // Initialize State
        }

        public void SetFilter(int ep, int ch, int sec, string secName = null)
        {
            FilterEpisode = ep;
            FilterChapter = ch;
            FilterSection = sec;
            FilterSectionName = secName;
            State.ScrollPos = Vector2.zero;
        }

        public void Draw(Rect position, float sidebarWidth = 0f)
        {
            if (Data == null) return;

            // Define the canvas area for clipping
            GUI.BeginGroup(position);
            Rect localRect = new Rect(0, 0, position.width, position.height);

            // Draw Background & Grid
            // Apply Zoom to World Space
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(Zoom, Zoom), Vector2.zero);

            // Calculate Visible World Area
            Rect worldRect = new Rect(0, 0, localRect.width / Zoom, localRect.height / Zoom);

            DrawGrid(worldRect, 20, 0.2f, Color.gray);
            DrawGrid(worldRect, 100, 0.4f, Color.gray);

            // Update Cache for Connections
            UpdatePosCache();

            // 1. Draw Connections
            if (Event.current.type == EventType.Repaint)
            {
                DrawConnections(localRect);
            }

            // 2. Draw Nodes
            for (int i = 0; i < Data.Conversations.Count; i++)
            {
                var unit = Data.Conversations[i];
                if (!IsUnitVisible(unit)) continue;

                if (unit.Position == Vector2.zero) unit.Position = new Vector2(100 + (i * 20), 100 + (i * 20));

                Rect nodeRect = GetNodeRect(unit, State.ScrollPos);

                DrawNode(unit, nodeRect);
            }

            // Restore Matrix for UI Overlay
            GUI.matrix = oldMatrix;

            // 3. Draw Overlay
            DrawOverlay(sidebarWidth);

            // Draw Selection Box
            if (State.IsDraggingSelectionBox)
            {
                // Box is drawn in Screen Space (mousePos is raw), so we might need logic here.
                // Actually, StartDraggingSelectionBox uses 'mousePos'.
                // If we change 'mousePos' to be World Space in ProcessEvents, then 'SelectionRect' will be World Space.
                // So we should draw it in World Space (inside the Matrix).

                // Let's Move this INSIDE the matrix for consistent World Space rendering.
                GUI.matrix = oldMatrix; // Undo first
                GUIUtility.ScaleAroundPivot(new Vector2(Zoom, Zoom), Vector2.zero); // Re-apply

                GUI.Box(State.SelectionRect, "", "SelectionRect");

                GUI.matrix = oldMatrix; // Restore again
            }

            // Process Input LAST
            ProcessEvents(Event.current, localRect, sidebarWidth);

            // Handle Deletions
            if (_nodesToDelete.Count > 0)
            {
                foreach (var node in _nodesToDelete)
                {
                    Data.Conversations.Remove(node);

                    // Cleanup References (Fix "Tail" issue)
                    foreach (var other in Data.Conversations)
                    {
                        if (other.NextID == node.ID) other.NextID = null;

                        if (other.Choices != null)
                        {
                            foreach (var c in other.Choices)
                            {
                                if (c.TargetID == node.ID) c.TargetID = null;
                            }
                        }
                    }
                }
                _nodesToDelete.Clear();
            }

            GUI.EndGroup();
        }

        public Vector2 ScrollPos
        {
            get => State.ScrollPos;
            set => State.ScrollPos = value;
        }

        public float Zoom
        {
            get => State.Zoom;
            set => State.Zoom = value;
        }

        private void DrawOverlay(float sidebarWidth)
        {
            if (FilterEpisode != int.MinValue || FilterChapter != int.MinValue || FilterSection != int.MinValue)
            {
                var sample = Data.Conversations.FirstOrDefault(u => IsUnitVisible(u));
                string secDisplay = sample?.SectionName ?? (FilterSection != int.MinValue ? FilterSection.ToString() : "All");

                string label = $"Ep:{(FilterEpisode != int.MinValue ? FilterEpisode.ToString() : "All")} / Ch:{(FilterChapter != int.MinValue ? FilterChapter.ToString() : "All")} / {secDisplay}";
                GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(1f, 1f, 1f, 0.3f);

                float x = (sidebarWidth > 0 ? sidebarWidth : 0) + 20;
                GUI.Label(new Rect(x, 20, 600, 50), label, style);
            }

            // Debug Overlay
            if (_host != null)
            {
                Rect hostRect = _host.position;
                Rect localRect = new Rect(0, 0, hostRect.width, hostRect.height - EditorStyles.toolbar.fixedHeight);
                Rect worldRect = new Rect(0, 0, localRect.width / Zoom, localRect.height / Zoom);

                string debugInfo = $"Window: {hostRect.width}x{hostRect.height}\n" +
                                   $"Editor: {localRect.width}x{localRect.height}\n" +
                                   $"Grid: {worldRect.width}x{worldRect.height}\n" +
                                   $"Zoom: {Zoom:F2}";

                GUIStyle debugStyle = new GUIStyle(EditorStyles.label);
                debugStyle.alignment = TextAnchor.LowerRight;
                debugStyle.normal.textColor = Color.yellow;

                // Draw at bottom right of the canvas (relative to 0,0 of the group)
                // Since we are in BeginGroup(mainArea), the coordinate (localRect.width, localRect.height) is the bottom right.
                Rect debugRect = new Rect(localRect.width - 300, localRect.height - 100, 290, 90);
                GUI.Label(debugRect, debugInfo, debugStyle);
            }
        }

        private bool IsUnitVisible(ConversationUnit unit)
        {
            if (FilterEpisode == int.MinValue && FilterChapter == int.MinValue && FilterSection == int.MinValue) return true;

            bool matchEp = FilterEpisode == int.MinValue || unit.EpisodeID == FilterEpisode;
            bool matchCh = FilterChapter == int.MinValue || unit.ChapterID == FilterChapter;
            bool matchSec = FilterSection == int.MinValue || unit.SectionID == FilterSection;

            // If strict name filter is set, check it
            bool matchName = true;
            if (!string.IsNullOrEmpty(FilterSectionName))
            {
                matchName = unit.SectionName == FilterSectionName;
            }

            return matchEp && matchCh && matchSec && matchName;
        }

        private void DrawNode(ConversationUnit unit, Rect rect)
        {
            Color nodeColor = Data.GetActorColor(unit.SpeakerName);

            // Selection Highlight (Cyan Border)
            if (State.IsSelected(unit.ID))
            {
                float border = 2f;
                Rect selectionRect = new Rect(rect.x - border, rect.y - border, rect.width + border * 2, rect.height + border * 2);
                EditorGUI.DrawRect(selectionRect, Color.cyan);
            }

            // 1. Definition Border (Black)
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));

            // 2. Main Background (In set by 1px for border effect)
            Rect bodyRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            EditorGUI.DrawRect(bodyRect, nodeColor);

            // Close Button (Red)
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUI.Button(new Rect(rect.x + rect.width - 20, rect.y, 20, 20), "×"))
            {
                _nodesToDelete.Add(unit);
            }
            GUI.backgroundColor = oldBg;

            Rect contentRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

            GUILayout.BeginArea(contentRect);
            EditorGUILayout.BeginVertical();

            // Speaker (Dropdown)
            var actorNames = Data.ActorDefinitions.Select(a => a.Name).ToList();

            // Shared Input Color (Actor Color - Brighter)
            // Keeping standard rounded UI but tinting it with the actor's color (mixed with white for brightness).
            Color inputBgColor = Color.Lerp(nodeColor, Color.white, 0f);

            if (actorNames.Count > 0)
            {
                // Ensure current name is in list
                if (!string.IsNullOrEmpty(unit.SpeakerName) && !actorNames.Contains(unit.SpeakerName))
                {
                    actorNames.Insert(0, unit.SpeakerName);
                }
                else if (string.IsNullOrEmpty(unit.SpeakerName))
                {
                    if (!actorNames.Contains("")) actorNames.Insert(0, "");
                }

                int currentIndex = actorNames.IndexOf(unit.SpeakerName ?? "");
                if (currentIndex == -1) currentIndex = 0;

                Color dropdownBg = GUI.backgroundColor;
                GUI.backgroundColor = inputBgColor;
                int newIndex = EditorGUILayout.Popup(currentIndex, actorNames.ToArray(), GUILayout.Width(80));
                GUI.backgroundColor = dropdownBg;

                if (newIndex >= 0 && newIndex < actorNames.Count)
                {
                    unit.SpeakerName = actorNames[newIndex];
                }
            }
            else
            {
                // Fallback if no ActorList defined
                Color dropdownBg = GUI.backgroundColor;
                GUI.backgroundColor = inputBgColor;
                unit.SpeakerName = EditorGUILayout.TextField(unit.SpeakerName, GUILayout.Width(80));
                GUI.backgroundColor = dropdownBg;
            }
            GUILayout.Space(3);

            // Body Text
            Color bodyBg = GUI.backgroundColor;
            GUI.backgroundColor = inputBgColor;
            unit.BodyText = EditorGUILayout.TextArea(unit.BodyText, GUILayout.Height(55));
            GUI.backgroundColor = bodyBg;

            // Choices Section
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel, GUILayout.Width(60));
            // Add Choice Button (Header style)
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                if (unit.Choices == null) unit.Choices = new List<Choice>();
                unit.Choices.Add(new Choice { ChoiceText = "New Choice" });
            }
            EditorGUILayout.EndHorizontal();

            if (unit.Choices == null) unit.Choices = new List<Choice>();

            for (int i = 0; i < unit.Choices.Count; i++)
            {
                var choice = unit.Choices[i];
                EditorGUILayout.BeginHorizontal();

                // Choice Text
                GUI.backgroundColor = inputBgColor;
                choice.ChoiceText = EditorGUILayout.TextField(choice.ChoiceText);
                GUI.backgroundColor = bodyBg;

                // Create/Link Node Button
                if (string.IsNullOrEmpty(choice.TargetID))
                {
                    if (GUILayout.Button("+Node", GUILayout.Width(45)))
                    {
                        CreateNodeForChoice(unit, choice);
                    }
                }
                else
                {
                    // Visual indicator that it is linked
                    // Could add a "Jump to" or "Clear" but keeping it simple for now
                    if (GUILayout.Button("->", GUILayout.Width(25)))
                    {
                        // Select target?
                        State.SetSelection(choice.TargetID);
                        // Pan to target?
                        var target = Data.Conversations.FirstOrDefault(x => x.ID == choice.TargetID);
                        if (target != null) State.ScrollPos = target.Position - new Vector2(250, 50);
                    }
                }

                // Delete Choice
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    unit.Choices.RemoveAt(i);
                    i--;
                }
                GUI.backgroundColor = bodyBg;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void CreateNodeForChoice(ConversationUnit parent, Choice choice)
        {
            string newID = GenerateNextID();

            // Heuristic position for new node:
            // Place it to the right and slightly down from parent, or stack them if multiple choices
            int choiceIndex = parent.Choices.IndexOf(choice);
            Vector2 offset = new Vector2(NodeWidth + 50, (choiceIndex * (NodeHeight + 20)));
            Vector2 newPos = parent.Position + offset;

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = parent.EpisodeID,
                ChapterID = parent.ChapterID,
                SectionID = parent.SectionID,
                SpeakerName = "New Speaker",
                BodyText = "New Text",
                Position = newPos
            };

            choice.TargetID = newID;
            Data.Conversations.Add(newUnit);
            _host.Repaint();
        }

        private void ProcessEvents(Event e, Rect viewRect, float restrictedX = 0f)
        {
            // Zoom: Modify mousePos to likely World Coordinate relative to Zoom
            // Pivot is (0,0) of the Group.
            Vector2 mousePos = e.mousePosition / Zoom;

            if (restrictedX > 0 && e.mousePosition.x < restrictedX) return;

            // Zoom Control
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                float oldZoom = Zoom;
                float newZoom = Mathf.Clamp(oldZoom + zoomDelta, 0.2f, 2.0f);

                if (Mathf.Abs(newZoom - oldZoom) > 0.001f)
                {
                    // Mouse-Centered Zoom Logic
                    // CanvasPos = (ViewPos / oldZoom) + ScrollPos
                    Vector2 mouseViewPos = e.mousePosition; // Raw View Pos
                    Vector2 mouseCanvasPos = (mouseViewPos / oldZoom) + State.ScrollPos;

                    Zoom = newZoom;

                    // newScrollPos = mouseCanvasPos - (mouseViewPos / newZoom)
                    State.ScrollPos = mouseCanvasPos - (mouseViewPos / newZoom);

                    e.Use();
                }
                return;
            }

            // Delete Key
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                if (State.SelectedIDs.Count > 0)
                {
                    // Create a copy to modify collection safely if needed, though we track units here
                    var idsToDelete = State.SelectedIDs.ToList();
                    foreach (var id in idsToDelete)
                    {
                        var u = Data.Conversations.FirstOrDefault(x => x.ID == id);
                        if (u != null) _nodesToDelete.Add(u);
                    }
                    State.ClearSelection();
                    e.Use();
                    _host.Repaint();
                }
                return;
            }

            // Hit Test
            string clickedNodeID = GetNodeAtPosition(mousePos);
            bool isOverNode = !string.IsNullOrEmpty(clickedNodeID);

            // Right Click (Context Menu)
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                var menu = new GenericMenu();
                if (isOverNode)
                {
                    var unit = Data.Conversations.FirstOrDefault(u => u.ID == clickedNodeID);
                    if (unit != null)
                    {
                        // Select the node we right-clicked if not already selected
                        if (!State.IsSelected(clickedNodeID)) State.SetSelection(clickedNodeID);

                        // "Add Node" becomes contextual: It inserts after this node
                        menu.AddItem(new GUIContent("Add Node"), false, () => InsertNodeAfter(unit));

                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Node"), false, () => _nodesToDelete.Add(unit));
                    }
                }
                else
                {
                    menu.AddItem(new GUIContent("Add Node"), false, () => CreateNode(mousePos + State.ScrollPos));
                }
                menu.ShowAsContext();
                e.Use();
                return;
            }

            // Left Click Logic
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (isOverNode)
                {
                    // Selection Logic
                    if (e.modifiers == EventModifiers.Shift || e.modifiers == EventModifiers.Control)
                    {
                        if (State.IsSelected(clickedNodeID)) State.RemoveFromSelection(clickedNodeID);
                        else State.AddToSelection(clickedNodeID);
                    }
                    else
                    {
                        if (!State.IsSelected(clickedNodeID))
                        {
                            State.SetSelection(clickedNodeID);
                        }
                    }

                    State.StartDraggingNode(clickedNodeID, mousePos);
                    e.Use();
                }
                else
                {
                    // Start Box Select
                    State.StartDraggingSelectionBox(mousePos);
                    if (e.modifiers != EventModifiers.Shift && e.modifiers != EventModifiers.Control)
                    {
                        State.ClearSelection();
                    }
                    e.Use();
                }
            }

            // Dragging
            if (e.type == EventType.MouseDrag)
            {
                if (State.IsDraggingNode)
                {
                    Vector2 delta = mousePos - State.LastMousePos;
                    MoveSelectedNodes(delta);
                    State.LastMousePos = mousePos;

                    e.Use();
                    _host.Repaint();
                }
                else if (State.IsDraggingSelectionBox)
                {
                    State.UpdateSelectionBox(mousePos);
                    SelectNodesInRect(State.SelectionRect, State.ScrollPos);
                    e.Use();
                    _host.Repaint();
                }
                else if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    State.ScrollPos -= e.delta;
                    e.Use();
                    _host.Repaint();
                }
            }

            if (e.type == EventType.MouseUp)
            {
                State.StopDraggingNode();
                State.StopDraggingSelectionBox();
            }
        }

        private string GetNodeAtPosition(Vector2 mousePos)
        {
            for (int i = Data.Conversations.Count - 1; i >= 0; i--)
            {
                var unit = Data.Conversations[i];
                if (!IsUnitVisible(unit)) continue;

                Rect nodeRect = GetNodeRect(unit, State.ScrollPos);

                if (nodeRect.Contains(mousePos)) return unit.ID;
            }
            return null;
        }

        private Rect GetNodeRect(ConversationUnit unit, Vector2 scrollPos)
        {
            float currentHeight = 115f; // reduced base height since button is moved up
            if (unit.Choices != null && unit.Choices.Count > 0)
            {
                currentHeight += unit.Choices.Count * 25f;
            }

            Vector2 drawPos = unit.Position - scrollPos;
            return new Rect(drawPos.x, drawPos.y, NodeWidth, currentHeight);
        }

        private void CreateNode(Vector2 worldPos)
        {
            string newID = GenerateNextID();

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = FilterEpisode != int.MinValue ? FilterEpisode : 1,
                ChapterID = FilterChapter != int.MinValue ? FilterChapter : 1,
                SectionID = FilterSection != int.MinValue ? FilterSection : 1,
                SpeakerName = "New Speaker",
                BodyText = "New Text",
                Position = worldPos
            };

            Data.Conversations.Add(newUnit);
            _host.Repaint();
        }

        private void InsertNodeAfter(ConversationUnit parent)
        {
            string newID = GenerateNextID();

            // Place BELOW the parent by default for vertical flow.
            Vector2 newPos = parent.Position + new Vector2(0, NodeHeight + 50);

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = parent.EpisodeID,
                ChapterID = parent.ChapterID,
                SectionID = parent.SectionID,
                SpeakerName = "New Speaker",
                BodyText = "New Text",
                Position = newPos,
                NextID = parent.NextID // Inherit the flow
            };

            parent.NextID = newID;

            Data.Conversations.Add(newUnit);
            _host.Repaint();
        }

        private string GenerateNextID()
        {
            // Fallback for ID generation prefix logic if filters are not set
            int ep = FilterEpisode != int.MinValue ? FilterEpisode : 1;
            int ch = FilterChapter != int.MinValue ? FilterChapter : 1;
            int sec = FilterSection != int.MinValue ? FilterSection : 1;

            string prefix = $"{ep}_{ch}_{sec}_";

            int maxNum = 0;
            foreach (var u in Data.Conversations)
            {
                if (u.ID != null && u.ID.StartsWith(prefix))
                {
                    string suffix = u.ID.Substring(prefix.Length);
                    if (int.TryParse(suffix, out int n))
                    {
                        if (n > maxNum) maxNum = n;
                    }
                }
            }
            return prefix + (maxNum + 1);
        }

        private Rect GetRect(Vector2 p1, Vector2 p2)
        {
            return Rect.MinMaxRect(
                Mathf.Min(p1.x, p2.x),
                Mathf.Min(p1.y, p2.y),
                Mathf.Max(p1.x, p2.x),
                Mathf.Max(p1.y, p2.y)
            );
        }

        private void MoveSelectedNodes(Vector2 delta)
        {
            foreach (var unit in Data.Conversations)
            {
                if (State.IsSelected(unit.ID))
                {
                    unit.Position += delta;
                }
            }
        }

        private void SelectNodesInRect(Rect r, Vector2 scrollPos)
        {
            // r is in "Group Local" space.
            // unit.Position is in World space.
            // Node is drawn at (unit.Position - ScrollPos).
            // So we compare r with Rect(unit.Position - ScrollPos, size).

            foreach (var unit in Data.Conversations)
            {
                if (!IsUnitVisible(unit)) continue;

                Rect nodeRect = GetNodeRect(unit, scrollPos);
                if (r.Overlaps(nodeRect))
                {
                    State.AddToSelection(unit.ID);
                }
            }
        }

        private void UpdatePosCache()
        {
            _posCache.Clear();
            foreach (var unit in Data.Conversations)
            {
                if (!string.IsNullOrEmpty(unit.ID)) _posCache[unit.ID] = unit.Position;
            }
        }

        public void AutoLayoutFlow(float extraOffsetX = 0f)
        {
            var visibleNodes = Data.Conversations.Where(IsUnitVisible).ToList();
            if (visibleNodes.Count == 0) return;

            // 1. Calculate Levels
            var levels = GetNodeLevels(visibleNodes);

            // 2. Group by Level
            var nodesByLevel = new Dictionary<int, List<ConversationUnit>>();
            foreach (var node in visibleNodes)
            {
                int lvl = levels[node.ID];
                if (!nodesByLevel.ContainsKey(lvl)) nodesByLevel[lvl] = new List<ConversationUnit>();
                nodesByLevel[lvl].Add(node);
            }

            Vector2 startOffset = new Vector2(50, 50);
            float spacingX = NodeWidth + 80f; // Wider for connections
            float spacingY = NodeHeight + 20f; // Reduced gap for shorter connectors

            // Sort layers by ID
            foreach (var lvl in nodesByLevel.Keys)
            {
                nodesByLevel[lvl].Sort(CompareNodeIDs);
            }

            foreach (var kvp in nodesByLevel)
            {
                int level = kvp.Key;
                var layerNodes = kvp.Value;

                float y = level * spacingY;
                for (int i = 0; i < layerNodes.Count; i++)
                {
                    float x = i * spacingX;
                    layerNodes[i].Position = startOffset + new Vector2(x, y);
                }
            }

            if (visibleNodes.Count > 0) State.ScrollPos = visibleNodes[0].Position - new Vector2(50 + extraOffsetX, 50);
        }

        public void AutoLayoutVisibleNodes(float extraOffsetX = 0f)
        {
            var visibleNodes = Data.Conversations.Where(IsUnitVisible).ToList();
            if (visibleNodes.Count == 0) return;

            // 1. Calculate Levels and Sort by Level then ID
            var levels = GetNodeLevels(visibleNodes);

            visibleNodes.Sort((a, b) =>
            {
                int levelA = levels.ContainsKey(a.ID) ? levels[a.ID] : 0;
                int levelB = levels.ContainsKey(b.ID) ? levels[b.ID] : 0;

                if (levelA != levelB) return levelA.CompareTo(levelB);
                return CompareNodeIDs(a, b);
            });

            int count = visibleNodes.Count;
            // Calculate grid dimensions (Column-Major: Fill Top->Down, then Right)
            int rows = Mathf.CeilToInt(Mathf.Sqrt(count));
            // Ensure at least 1 row to prevent divide by zero
            rows = Mathf.Max(1, rows);

            float spacingX = NodeWidth + 50f;
            float spacingY = NodeHeight + 30f;

            // Start offset to avoid (0,0) which is treated as "uninitialized" in Draw()
            Vector2 startOffset = new Vector2(50, 50);

            for (int i = 0; i < count; i++)
            {
                // Column-Major indices
                int row = i % rows;
                int col = i / rows;

                visibleNodes[i].Position = startOffset + new Vector2(col * spacingX, row * spacingY);
            }

            if (visibleNodes.Count > 0) State.ScrollPos = visibleNodes[0].Position - new Vector2(50 + extraOffsetX, 50);
        }

        private Dictionary<string, int> GetNodeLevels(List<ConversationUnit> nodes)
        {
            Dictionary<string, List<string>> adjacency = new Dictionary<string, List<string>>();
            Dictionary<string, int> inDegree = new Dictionary<string, int>();
            Dictionary<string, ConversationUnit> nodeMap = new Dictionary<string, ConversationUnit>();

            foreach (var n in nodes)
            {
                nodeMap[n.ID] = n;
                if (!adjacency.ContainsKey(n.ID)) adjacency[n.ID] = new List<string>();
                if (!inDegree.ContainsKey(n.ID)) inDegree[n.ID] = 0;
            }

            foreach (var n in nodes)
            {
                List<string> targets = new List<string>();
                if (!string.IsNullOrEmpty(n.NextID)) targets.Add(n.NextID);
                if (n.Choices != null)
                {
                    foreach (var c in n.Choices) if (!string.IsNullOrEmpty(c.TargetID)) targets.Add(c.TargetID);
                }

                foreach (var t in targets)
                {
                    if (nodeMap.ContainsKey(t))
                    {
                        adjacency[n.ID].Add(t);
                        inDegree[t]++;
                    }
                }
            }

            // Roots
            Dictionary<string, int> levels = new Dictionary<string, int>();
            Queue<string> queue = new Queue<string>();

            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                    levels[kvp.Key] = 0;
                }
            }

            // Cycle fallback
            if (queue.Count == 0 && nodes.Count > 0)
            {
                // Sort to pick deterministically if fully cyclic
                nodes.Sort(CompareNodeIDs);
                var first = nodes[0].ID;
                queue.Enqueue(first);
                levels[first] = 0;
            }

            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                int currentLevel = levels[id];

                if (adjacency.ContainsKey(id))
                {
                    foreach (var childID in adjacency[id])
                    {
                        if (!levels.ContainsKey(childID))
                        {
                            levels[childID] = currentLevel + 1;
                            queue.Enqueue(childID);
                        }
                        else
                        {
                            if (levels[childID] < currentLevel + 1)
                            {
                                levels[childID] = currentLevel + 1;
                                if (levels[childID] < nodes.Count)
                                    queue.Enqueue(childID);
                            }
                        }
                    }
                }
            }

            // Fill unvisited
            foreach (var n in nodes)
            {
                if (!levels.ContainsKey(n.ID)) levels[n.ID] = 0;
            }

            return levels;
        }

        private int CompareNodeIDs(ConversationUnit a, ConversationUnit b)
        {
            // Try to extract suffix numbers
            string idA = a.ID ?? "";
            string idB = b.ID ?? "";

            var partsA = idA.Split('_');
            var partsB = idB.Split('_');

            if (partsA.Length > 0 && partsB.Length > 0)
            {
                // Compare Last Parts as int if possible
                string suffixA = partsA[partsA.Length - 1];
                string suffixB = partsB[partsB.Length - 1];

                if (int.TryParse(suffixA, out int numA) && int.TryParse(suffixB, out int numB))
                {
                    // If prefixes are same, sort by number
                    // Construct prefix from all parts except last
                    string prefixA = string.Join("_", partsA.Take(partsA.Length - 1));
                    string prefixB = string.Join("_", partsB.Take(partsB.Length - 1));

                    int prefixCompare = string.Compare(prefixA, prefixB);
                    if (prefixCompare != 0) return prefixCompare;

                    return numA.CompareTo(numB);
                }
            }

            // Fallback to Natural Sort
            return EditorUtility.NaturalCompare(idA, idB);
        }

        private void DrawConnections(Rect visibleRect)
        {
            foreach (var unit in Data.Conversations)
            {
                if (!IsUnitVisible(unit)) continue;

                // Use dynamic rect for connection start point
                Rect nodeRect = GetNodeRect(unit, State.ScrollPos);

                // Normal NextID starts from Bottom-Center
                Vector2 bottomStartPos = new Vector2(nodeRect.center.x, nodeRect.yMax);

                if (!string.IsNullOrEmpty(unit.NextID))
                {
                    var target = Data.Conversations.FirstOrDefault(u => u.ID == unit.NextID);
                    if (target != null && IsUnitVisible(target))
                    {
                        DrawCurve(bottomStartPos, unit.NextID, Color.white, false);
                    }
                }

                if (unit.Choices != null)
                {
                    // Choice connections start from Right Edge
                    // Base Offset Calculation:
                    // Header(~86px) + Choices Label(~20px) = ~106px (approx start of first choice)
                    // Choice Row = 25px
                    float choicesStartY = nodeRect.y + 106f;

                    for (int i = 0; i < unit.Choices.Count; i++)
                    {
                        var c = unit.Choices[i];
                        if (!string.IsNullOrEmpty(c.TargetID))
                        {
                            var target = Data.Conversations.FirstOrDefault(u => u.ID == c.TargetID);
                            if (target != null && IsUnitVisible(target))
                            {
                                // Center of the choice row
                                float choiceRowCenterY = choicesStartY + (i * 25f) + 12.5f;
                                Vector2 choiceStartPos = new Vector2(nodeRect.xMax, choiceRowCenterY);

                                DrawCurve(choiceStartPos, c.TargetID, Color.cyan, true);
                            }
                        }
                    }
                }
            }
        }

        private void DrawCurve(Vector2 start, string targetID, Color color, bool startFromRight)
        {
            // Target Node top center
            if (Data.Conversations.FirstOrDefault(u => u.ID == targetID) is ConversationUnit targetUnit)
            {
                // We need target position relative to scroll
                // We don't need full rect, just top center.
                // Target Pos is Top-Left. Width is fixed (NodeWidth).
                Vector2 targetWorldPos = targetUnit.Position;
                Vector2 targetDrawPos = targetWorldPos - State.ScrollPos;
                Vector2 end = targetDrawPos + new Vector2(NodeWidth / 2, 0);

                // Tangents
                Vector2 startTangent = start + (startFromRight ? Vector2.right : Vector2.up) * 50;
                Vector2 endTangent = end + Vector2.down * 50;

                Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 2f);
            }
        }

        private void DrawGrid(Rect rect, float spacing, float opacity, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;

            Handles.BeginGUI();
            Handles.color = new Color(color.r, color.g, color.b, opacity);

            int widthDivs = Mathf.CeilToInt(rect.width / spacing);
            int heightDivs = Mathf.CeilToInt(rect.height / spacing);

            for (int i = 0; i < widthDivs; i++)
            {
                float x = (spacing * i) - (State.ScrollPos.x % spacing);
                if (x < 0) x += spacing;
                Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, rect.height, 0));
            }

            for (int j = 0; j < heightDivs; j++)
            {
                float y = (spacing * j) - (State.ScrollPos.y % spacing);
                if (y < 0) y += spacing;
                Handles.DrawLine(new Vector3(0, y, 0), new Vector3(rect.width, y, 0));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }
    }
}
