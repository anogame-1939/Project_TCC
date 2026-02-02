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

        // Filter (-1 means All)
        public int FilterEpisode = -1;
        public int FilterChapter = -1;
        public int FilterSection = -1;

        // Actions
        private List<ConversationUnit> _nodesToDelete = new List<ConversationUnit>();

        public DialogueGraphCanvas(MasterDialogueData data, EditorWindow host)
        {
            Data = data;
            _host = host;
            State = new DialogueGraphState(); // Initialize State
        }

        public void SetFilter(int episode, int chapter, int section)
        {
            FilterEpisode = episode;
            FilterChapter = chapter;
            FilterSection = section;
            State.ClearSelection();
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
            if (FilterEpisode != -1 || FilterChapter != -1 || FilterSection != -1)
            {
                var sample = Data.Conversations.FirstOrDefault(u => IsUnitVisible(u));
                // Show SectionName if available, else just "Section X"
                string secDisplay = sample?.SectionName ?? (FilterSection != -1 ? FilterSection.ToString() : "All");

                string epStr = FilterEpisode != -1 ? FilterEpisode.ToString() : "All";
                string chStr = FilterChapter != -1 ? FilterChapter.ToString() : "All";

                string label = $"{epStr} / {chStr} / {secDisplay}";
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
                Rect debugRect = new Rect(localRect.width - 300, localRect.height - 100, 290, 90);
                GUI.Label(debugRect, debugInfo, debugStyle);
            }
        }

        private bool IsUnitVisible(ConversationUnit unit)
        {
            if (FilterEpisode == -1 && FilterChapter == -1 && FilterSection == -1) return true;
            bool matchEp = FilterEpisode == -1 || unit.EpisodeID == FilterEpisode;
            bool matchCh = FilterChapter == -1 || unit.ChapterID == FilterChapter;
            bool matchSec = FilterSection == -1 || unit.SectionID == FilterSection;
            return matchEp && matchCh && matchSec;
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
            int choiceIndex = parent.Choices.IndexOf(choice);
            Vector2 offset = new Vector2(NodeWidth + 50, (choiceIndex * (NodeHeight + 20)));
            Vector2 newPos = parent.Position + offset;

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = parent.EpisodeID,
                ChapterID = parent.ChapterID,
                SectionID = parent.SectionID,
                SectionName = parent.SectionName, // inherit section name?
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
                    Vector2 mouseViewPos = e.mousePosition; // Raw View Pos
                    Vector2 mouseCanvasPos = (mouseViewPos / oldZoom) + State.ScrollPos;

                    Zoom = newZoom;

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
                EpisodeID = FilterEpisode != -1 ? FilterEpisode : 1, // Default to 1
                ChapterID = FilterChapter != -1 ? FilterChapter : 1,
                SectionID = FilterSection != -1 ? FilterSection : 1,
                SectionName = "New Section",
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

            // Place BELOW the parent by default
            Vector2 newPos = parent.Position + new Vector2(0, NodeHeight + 50);

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = parent.EpisodeID,
                ChapterID = parent.ChapterID,
                SectionID = parent.SectionID,
                SectionName = parent.SectionName,
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
            int ep = FilterEpisode != -1 ? FilterEpisode : 1;
            int ch = FilterChapter != -1 ? FilterChapter : 1;
            int sec = FilterSection != -1 ? FilterSection : 1;
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

            var levels = GetNodeLevels(visibleNodes);

            var nodesByLevel = new Dictionary<int, List<ConversationUnit>>();
            foreach (var node in visibleNodes)
            {
                int lvl = levels[node.ID];
                if (!nodesByLevel.ContainsKey(lvl)) nodesByLevel[lvl] = new List<ConversationUnit>();
                nodesByLevel[lvl].Add(node);
            }

            Vector2 startOffset = new Vector2(50, 50);
            float spacingX = NodeWidth + 80f;
            float spacingY = NodeHeight + 20f;

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

            var levels = GetNodeLevels(visibleNodes);

            visibleNodes.Sort((a, b) =>
            {
                int levelA = levels.ContainsKey(a.ID) ? levels[a.ID] : 0;
                int levelB = levels.ContainsKey(b.ID) ? levels[b.ID] : 0;
                if (levelA != levelB) return levelA.CompareTo(levelB);
                return CompareNodeIDs(a, b);
            });

            int count = visibleNodes.Count;
            int rows = Mathf.CeilToInt(Mathf.Sqrt(count));
            rows = Mathf.Max(1, rows);

            float spacingX = NodeWidth + 50f;
            float spacingY = NodeHeight + 30f;
            Vector2 startOffset = new Vector2(50, 50);

            for (int i = 0; i < count; i++)
            {
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

            if (queue.Count == 0 && nodes.Count > 0)
            {
                nodes.Sort(CompareNodeIDs);
                var first = nodes[0].ID;
                queue.Enqueue(first);
                levels[first] = 0;
            }

            while (queue.Count > 0)
            {
                string u = queue.Dequeue();
                int currentLevel = levels[u];

                if (adjacency.ContainsKey(u))
                {
                    foreach (var v in adjacency[u])
                    {
                        if (levels.ContainsKey(v)) continue;
                        levels[v] = currentLevel + 1;
                        queue.Enqueue(v);
                    }
                }
            }
            return levels;
        }

        private int CompareNodeIDs(ConversationUnit a, ConversationUnit b)
        {
            return string.Compare(a.ID, b.ID);
        }

        private void DrawGrid(Rect rect, float spacing, float opacity, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;

            Handles.color = new Color(color.r, color.g, color.b, opacity);
            Vector2 offset = new Vector2(State.ScrollPos.x % spacing, State.ScrollPos.y % spacing);

            float xStart = Mathf.Floor(rect.x / spacing) * spacing;
            for (float x = xStart; x < rect.x + rect.width; x += spacing)
            {
                Handles.DrawLine(new Vector3(x, rect.y, 0), new Vector3(x, rect.y + rect.height, 0));
            }

            float yStart = Mathf.Floor(rect.y / spacing) * spacing;
            for (float y = yStart; y < rect.y + rect.height; y += spacing)
            {
                Handles.DrawLine(new Vector3(rect.x, y, 0), new Vector3(rect.x + rect.width, y, 0));
            }

            Handles.color = Color.white;
        }

        private void DrawConnections(Rect visibleRect)
        {
            foreach (var unit in Data.Conversations)
            {
                if (!IsUnitVisible(unit)) continue;

                Vector2 startPos = unit.Position + new Vector2(NodeWidth, NodeHeight / 2f);

                if (!string.IsNullOrEmpty(unit.NextID))
                {
                    if (_posCache.TryGetValue(unit.NextID, out Vector2 targetPos))
                    {
                        DrawConnection(startPos, targetPos + new Vector2(0, NodeHeight / 2f), Color.white);
                    }
                }

                if (unit.Choices != null)
                {
                    for (int i = 0; i < unit.Choices.Count; i++)
                    {
                        var choice = unit.Choices[i];
                        if (!string.IsNullOrEmpty(choice.TargetID))
                        {
                            if (_posCache.TryGetValue(choice.TargetID, out Vector2 targetPos))
                            {
                                float yOffset = NodeHeight + 20 + (i * 25) + 10;
                                Vector2 choiceStart = unit.Position + new Vector2(NodeWidth, yOffset);
                                DrawConnection(choiceStart, targetPos + new Vector2(0, NodeHeight / 2f), Color.cyan);
                            }
                        }
                    }
                }
            }
        }

        private void DrawConnection(Vector2 start, Vector2 end, Color color)
        {
            Handles.DrawBezier(
                start,
                end,
                start + Vector2.right * 50f,
                end + Vector2.left * 50f,
                color,
                null,
                2f
            );
        }
    }
}
