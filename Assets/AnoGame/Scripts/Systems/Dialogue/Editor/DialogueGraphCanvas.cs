using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Systems.Dialogue.Editor
{
    public class DialogueGraphCanvas
    {
        public MasterDialogueData Data;
        public Vector2 ScrollPos;
        public float Zoom = 1.0f;

        private EditorWindow _host;

        private const float NodeWidth = 255f;
        private const float NodeHeight = 92f; // Increased by another 5px

        // Selection
        private HashSet<string> _selectedIDs = new HashSet<string>();

        // Dragging Logic
        private bool _isDraggingSelectionBox = false;
        private Vector2 _selectionStartPos;
        private Rect _selectionRect;

        private bool _isDraggingNode = false;
        private string _draggingNodeID = null;
        private Vector2 _lastMousePos;

        // Caching
        private Dictionary<string, Vector2> _posCache = new Dictionary<string, Vector2>();

        public DialogueGraphCanvas(MasterDialogueData data, EditorWindow host)
        {
            Data = data;
            _host = host;
        }

        // Filter
        public string FilterChapter = null;
        public string FilterSection = null;

        public void SetFilter(string chapter, string section)
        {
            FilterChapter = chapter;
            FilterSection = section;
            _selectedIDs.Clear(); // Clear selection when changing views to avoid confusion
        }

        // Actions
        private List<ConversationUnit> _nodesToDelete = new List<ConversationUnit>();

        public void Draw(Rect position, float sidebarWidth = 0f)
        {
            if (Data == null) return;

            // Define the canvas area for clipping
            GUI.BeginGroup(position);
            Rect localRect = new Rect(0, 0, position.width, position.height);

            // Draw Background & Grid
            DrawGrid(localRect, 20, 0.2f, Color.gray);
            DrawGrid(localRect, 100, 0.4f, Color.gray);

            // Update Cache for Connections
            UpdatePosCache();

            // 1. Draw Connections (Behind nodes)
            if (Event.current.type == EventType.Repaint)
            {
                DrawConnections(localRect);
            }

            // 2. Draw Nodes (Custom Box)
            for (int i = 0; i < Data.Conversations.Count; i++)
            {
                var unit = Data.Conversations[i];
                if (!IsUnitVisible(unit)) continue;

                if (unit.Position == Vector2.zero) unit.Position = new Vector2(100 + (i * 20), 100 + (i * 20));

                Vector2 drawPos = unit.Position - ScrollPos;
                Rect nodeRect = new Rect(drawPos.x, drawPos.y, NodeWidth, NodeHeight);

                DrawNode(unit, nodeRect);
            }

            // 3. Draw Overlay (Section Name)
            DrawOverlay(sidebarWidth);

            // Draw Selection Box
            if (_isDraggingSelectionBox)
            {
                GUI.Box(_selectionRect, "", "SelectionRect");
            }

            // Process Input LAST
            ProcessEvents(Event.current, localRect, sidebarWidth);

            // Handle Deletions
            if (_nodesToDelete.Count > 0)
            {
                foreach (var node in _nodesToDelete)
                {
                    Data.Conversations.Remove(node);
                }
                _nodesToDelete.Clear();
            }

            GUI.EndGroup();
        }

        private void DrawOverlay(float sidebarWidth)
        {
            if (!string.IsNullOrEmpty(FilterChapter) || !string.IsNullOrEmpty(FilterSection))
            {
                string label = $"{FilterChapter ?? "All"} / {FilterSection ?? "All"}";
                GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
                style.fontSize = 24;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(1f, 1f, 1f, 0.3f); // Transparent white

                // Offset by sidebar width (+ padding) so it's not covered
                float x = (sidebarWidth > 0 ? sidebarWidth : 0) + 20;
                GUI.Label(new Rect(x, 20, 500, 50), label, style);
            }
        }

        private bool IsUnitVisible(ConversationUnit unit)
        {
            if (string.IsNullOrEmpty(FilterChapter) && string.IsNullOrEmpty(FilterSection)) return true;

            bool matchChapter = string.IsNullOrEmpty(FilterChapter) || unit.ChapterID == FilterChapter;
            bool matchSection = string.IsNullOrEmpty(FilterSection) || unit.SectionID == FilterSection;

            return matchChapter && matchSection;
        }

        private void DrawNode(ConversationUnit unit, Rect rect)
        {
            // Background Box
            GUIStyle style = new GUIStyle("window"); // Use default window style for look

            // Highlight
            if (_selectedIDs.Contains(unit.ID))
            {
                GUI.color = Color.cyan;
            }

            GUI.Box(rect, "", style);
            GUI.color = Color.white;

            // Delete Button (Top-Right)
            if (GUI.Button(new Rect(rect.x + rect.width - 20, rect.y, 20, 20), "×"))
            {
                _nodesToDelete.Add(unit);
            }

            // Content Area - Symmetric padding (5px top, 5px bottom)
            Rect contentRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

            GUILayout.BeginArea(contentRect);
            EditorGUILayout.BeginVertical();

            // Speaker (No Label)
            unit.SpeakerName = EditorGUILayout.TextField(unit.SpeakerName, GUILayout.Width(80));

            GUILayout.Space(3);

            // Text (No Label)
            unit.BodyText = EditorGUILayout.TextArea(unit.BodyText, GUILayout.Height(55));

            // Links Preview
            if (unit.Choices != null && unit.Choices.Count > 0)
            {
                EditorGUILayout.LabelField($"Choices: {unit.Choices.Count}", EditorStyles.miniLabel);
            }
            // NextID is hidden as requested

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void ProcessEvents(Event e, Rect viewRect, float restrictedX = 0f)
        {
            Vector2 mousePos = e.mousePosition; // Local to Group

            // If mouse is over the sidebar overlay area, ignore interaction with canvas
            // Note: Since Sidebar is overlaying the canvas, we assume restrictedX matches the visual sidebar width.
            if (restrictedX > 0 && mousePos.x < restrictedX) return;

            // Right Click (Context Menu)
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                string clickedNodeID = GetNodeAtPosition(mousePos);
                var menu = new GenericMenu();

                if (!string.IsNullOrEmpty(clickedNodeID))
                {
                    var unit = Data.Conversations.FirstOrDefault(u => u.ID == clickedNodeID);
                    if (unit != null)
                    {
                        menu.AddItem(new GUIContent("Add Node"), false, () => CreateNode(mousePos + ScrollPos));
                        menu.AddItem(new GUIContent("Insert Node After"), false, () => InsertNodeAfter(unit));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Node"), false, () => _nodesToDelete.Add(unit));
                    }
                }
                else
                {
                    menu.AddItem(new GUIContent("Add Node"), false, () => CreateNode(mousePos + ScrollPos));
                }

                menu.ShowAsContext();
                e.Use();
                return;
            }

            // Left Click (Selection / Dragging)
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                string clickedNodeID = GetNodeAtPosition(mousePos);

                if (!string.IsNullOrEmpty(clickedNodeID))
                {
                    // Clicked Node
                    if (e.modifiers == EventModifiers.Shift || e.modifiers == EventModifiers.Control)
                    {
                        // Toggle Selection
                        if (_selectedIDs.Contains(clickedNodeID)) _selectedIDs.Remove(clickedNodeID);
                        else _selectedIDs.Add(clickedNodeID);
                    }
                    else
                    {
                        // If not already selected, clear and select this
                        if (!_selectedIDs.Contains(clickedNodeID))
                        {
                            _selectedIDs.Clear();
                            _selectedIDs.Add(clickedNodeID);
                        }
                    }

                    _isDraggingNode = true;
                    _draggingNodeID = clickedNodeID;
                    _lastMousePos = e.mousePosition;
                    e.Use();
                }
                else
                {
                    // Clicked Empty -> Start Selection Box
                    _isDraggingSelectionBox = true;
                    _selectionStartPos = mousePos;
                    _selectionRect = new Rect(mousePos, Vector2.zero);

                    if (e.modifiers != EventModifiers.Shift && e.modifiers != EventModifiers.Control)
                    {
                        _selectedIDs.Clear();
                    }
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag)
            {
                if (_isDraggingNode)
                {
                    Vector2 legacyDelta = e.delta;
                    MoveSelectedNodes(legacyDelta, null);
                    _lastMousePos = mousePos;
                    e.Use();
                    _host.Repaint();
                }
                else if (_isDraggingSelectionBox)
                {
                    _selectionRect = GetRect(_selectionStartPos, mousePos);
                    SelectNodesInRect(_selectionRect, ScrollPos);
                    e.Use();
                    _host.Repaint();
                }
                else if (e.button == 2 || (e.button == 0 && e.alt)) // Middle or Alt+Left Pan
                {
                    ScrollPos -= e.delta;
                    e.Use();
                    _host.Repaint();
                }
            }

            if (e.type == EventType.MouseUp)
            {
                _isDraggingNode = false;
                _isDraggingSelectionBox = false;
                _draggingNodeID = null;
            }
        }

        private string GetNodeAtPosition(Vector2 mousePos)
        {
            // Iterate reverse to respect draw order (topmost first)
            for (int i = Data.Conversations.Count - 1; i >= 0; i--)
            {
                var unit = Data.Conversations[i];
                if (!IsUnitVisible(unit)) continue;

                Vector2 drawPos = unit.Position - ScrollPos;
                Rect nodeRect = new Rect(drawPos.x, drawPos.y, NodeWidth, NodeHeight);

                if (nodeRect.Contains(mousePos))
                {
                    return unit.ID;
                }
            }
            return null;
        }

        private void CreateNode(Vector2 worldPos)
        {
            string newID = GenerateNextID();

            var newUnit = new ConversationUnit
            {
                ID = newID,
                ChapterID = FilterChapter ?? "Chapter",
                SectionID = FilterSection ?? "Section",
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

            // Place to the right of parent (Flow direction is rightward? Or Downward?)
            // Wait, previous request set Flow to Vertical.
            // But connections go Bottom->Top.
            // Let's place it BELOW the parent by default for vertical flow.
            Vector2 newPos = parent.Position + new Vector2(0, NodeHeight + 50);

            var newUnit = new ConversationUnit
            {
                ID = newID,
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
            string chapter = FilterChapter ?? "Chapter";
            string section = FilterSection ?? "Section";
            string prefix = $"{chapter}_{section}_";

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

        private void MoveSelectedNodes(Vector2 delta, string excludeID)
        {
            foreach (var unit in Data.Conversations)
            {
                if (_selectedIDs.Contains(unit.ID))
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

                Vector2 drawPos = unit.Position - scrollPos;
                Rect nodeRect = new Rect(drawPos.x, drawPos.y, NodeWidth, NodeHeight);
                if (r.Overlaps(nodeRect))
                {
                    _selectedIDs.Add(unit.ID);
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

            if (visibleNodes.Count > 0) ScrollPos = visibleNodes[0].Position - new Vector2(50 + extraOffsetX, 50);
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

            if (visibleNodes.Count > 0) ScrollPos = visibleNodes[0].Position - new Vector2(50 + extraOffsetX, 50);
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

                Vector2 startPos = unit.Position - ScrollPos;
                startPos.x += NodeWidth / 2;
                startPos.y += NodeHeight;

                if (!string.IsNullOrEmpty(unit.NextID))
                {
                    var target = Data.Conversations.FirstOrDefault(u => u.ID == unit.NextID);
                    if (target != null && IsUnitVisible(target))
                    {
                        DrawCurve(startPos, unit.NextID, Color.white);
                    }
                }

                if (unit.Choices != null)
                {
                    foreach (var c in unit.Choices)
                    {
                        if (!string.IsNullOrEmpty(c.TargetID))
                        {
                            var target = Data.Conversations.FirstOrDefault(u => u.ID == c.TargetID);
                            if (target != null && IsUnitVisible(target))
                            {
                                DrawCurve(startPos, c.TargetID, Color.cyan);
                            }
                        }
                    }
                }
            }
        }

        private void DrawCurve(Vector2 start, string targetID, Color color)
        {
            if (_posCache.TryGetValue(targetID, out Vector2 targetPos))
            {
                Vector2 end = targetPos - ScrollPos;
                end.x += NodeWidth / 2;

                // Tangents: Start goes Down (+Y), End comes from Up (-Y)
                Handles.DrawBezier(start, end, start + Vector2.up * 50, end + Vector2.down * 50, color, null, 2f);
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
                float x = (spacing * i) - (ScrollPos.x % spacing);
                if (x < 0) x += spacing;
                Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, rect.height, 0));
            }

            for (int j = 0; j < heightDivs; j++)
            {
                float y = (spacing * j) - (ScrollPos.y % spacing);
                if (y < 0) y += spacing;
                Handles.DrawLine(new Vector3(0, y, 0), new Vector3(rect.width, y, 0));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }
    }
}
