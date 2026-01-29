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

        private const float NodeWidth = 220f;
        private const float NodeHeight = 150f;

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
            // Only draw connections if both start and end are visible? 
            // Or just start? Let's check visibility in loop.
            if (Event.current.type == EventType.Repaint)
            {
                DrawConnections(localRect);
            }

            // 2. Draw Nodes (Custom Box)
            for (int i = 0; i < Data.Conversations.Count; i++)
            {
                var unit = Data.Conversations[i];

                // Filter Check
                if (!IsUnitVisible(unit)) continue;

                if (unit.Position == Vector2.zero) unit.Position = new Vector2(100 + (i * 20), 100 + (i * 20));

                Vector2 drawPos = unit.Position - ScrollPos;
                Rect nodeRect = new Rect(drawPos.x, drawPos.y, NodeWidth, NodeHeight);

                // Simple Culling
                if (nodeRect.xMax < 0 || nodeRect.x > localRect.width || nodeRect.yMax < 0 || nodeRect.y > localRect.height)
                {
                    // Culling off-screen
                }

                DrawNode(unit, nodeRect);
            }

            // Draw Selection Box
            if (_isDraggingSelectionBox)
            {
                GUI.Box(_selectionRect, "", "SelectionRect");
            }

            // Process Input LAST to ensure it covers everything drawn
            ProcessEvents(Event.current, localRect, sidebarWidth);

            GUI.EndGroup();
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

            // Title Bar Area (for Dragging)
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 20);
            GUI.Label(titleRect, unit.ID, EditorStyles.boldLabel);

            // Content Area
            Rect contentRect = new Rect(rect.x + 5, rect.y + 20, rect.width - 10, rect.height - 25);

            GUILayout.BeginArea(contentRect);
            EditorGUILayout.BeginVertical();

            EditorGUIUtility.labelWidth = 50;
            unit.SpeakerName = EditorGUILayout.TextField("Speaker", unit.SpeakerName);

            EditorGUILayout.LabelField("Text:");
            unit.BodyText = EditorGUILayout.TextArea(unit.BodyText, GUILayout.Height(50));

            // Links Preview
            if (unit.Choices != null && unit.Choices.Count > 0)
            {
                EditorGUILayout.LabelField($"Choices: {unit.Choices.Count}", EditorStyles.miniLabel);
            }
            else if (!string.IsNullOrEmpty(unit.NextID))
            {
                EditorGUILayout.LabelField($"Next: {unit.NextID}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void ProcessEvents(Event e, Rect viewRect, float restrictedX = 0f)
        {
            Vector2 mousePos = e.mousePosition;

            // If mouse is over the sidebar overlay (restrictedX), ignore interaction with canvas
            if (mousePos.x < restrictedX) return;

            // Handle Node Dragging (Priority over box select)
            if (_isDraggingNode && e.type == EventType.MouseDrag)
            {
                Vector2 delta = mousePos - _lastMousePos;
                MoveSelectedNodes(delta, null); // Move all selected
                _lastMousePos = mousePos;
                GUI.changed = true;
                e.Use();
                return;
            }

            if (_isDraggingNode && e.type == EventType.MouseUp)
            {
                _isDraggingNode = false;
                _draggingNodeID = null;
                e.Use();
                return;
            }

            // Mouse Down on Canvas
            if (e.type == EventType.MouseDown && viewRect.Contains(mousePos))
            {
                if (e.button == 0) // Left Click
                {
                    // Check if clicked on a Node
                    string clickedNodeID = GetNodeAtPosition(mousePos);

                    if (clickedNodeID != null)
                    {
                        // Clicked a node
                        _isDraggingNode = true;
                        _draggingNodeID = clickedNodeID;
                        _lastMousePos = mousePos;

                        // Selection Logic
                        if (e.shift || e.control)
                        {
                            if (_selectedIDs.Contains(clickedNodeID)) _selectedIDs.Remove(clickedNodeID);
                            else _selectedIDs.Add(clickedNodeID);
                        }
                        else
                        {
                            if (!_selectedIDs.Contains(clickedNodeID))
                            {
                                _selectedIDs.Clear();
                                _selectedIDs.Add(clickedNodeID);
                            }
                            // If already selected, keep selection (to allow dragging group)
                        }

                        GUI.changed = true;
                        e.Use();
                    }
                    else
                    {
                        // Clicked Empty Space -> Start Box Select
                        _isDraggingSelectionBox = true;
                        _selectionStartPos = mousePos;
                        _selectionRect = new Rect(mousePos.x, mousePos.y, 0, 0);
                        if (!e.shift && !e.control) _selectedIDs.Clear();
                        e.Use();
                    }
                }
                else if (e.button == 2) // Middle Click -> Pan
                {
                    // Handled in Drag
                }
            }

            // Panning
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                ScrollPos -= e.delta;
                GUI.changed = true;
                e.Use();
            }

            // Box Selection Drag
            if (_isDraggingSelectionBox && e.type == EventType.MouseDrag)
            {
                _selectionRect = new Rect(
                    Mathf.Min(_selectionStartPos.x, mousePos.x),
                    Mathf.Min(_selectionStartPos.y, mousePos.y),
                    Mathf.Abs(mousePos.x - _selectionStartPos.x),
                    Mathf.Abs(mousePos.y - _selectionStartPos.y)
                );
                GUI.changed = true;
                e.Use();
            }

            // Box Selection End
            if (_isDraggingSelectionBox && e.type == EventType.MouseUp)
            {
                _isDraggingSelectionBox = false;
                SelectNodesInRect(_selectionRect, viewRect);
                e.Use();
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

        private void SelectNodesInRect(Rect r, Rect viewOffset)
        {
            Rect worldSelection = r;
            // r is in "Group Local" space.
            // unit.Position is in World space.
            // Node is drawn at (unit.Position - ScrollPos).
            // So we compare r with Rect(unit.Position - ScrollPos, size).

            foreach (var unit in Data.Conversations)
            {
                if (!IsUnitVisible(unit)) continue;

                Vector2 drawPos = unit.Position - ScrollPos;
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

        public void AutoLayoutFlow()
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
            float spacingY = NodeHeight + 30f;

            // Sort layers by ID
            foreach (var lvl in nodesByLevel.Keys)
            {
                nodesByLevel[lvl].Sort(CompareNodeIDs);
            }

            foreach (var kvp in nodesByLevel)
            {
                int level = kvp.Key;
                var layerNodes = kvp.Value;

                float x = level * spacingX;
                for (int i = 0; i < layerNodes.Count; i++)
                {
                    float y = i * spacingY;
                    layerNodes[i].Position = startOffset + new Vector2(x, y);
                }
            }

            if (visibleNodes.Count > 0) ScrollPos = visibleNodes[0].Position - new Vector2(50, 50);
        }

        public void AutoLayoutVisibleNodes()
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
            float spacingY = NodeHeight + 50f;

            // Start offset to avoid (0,0) which is treated as "uninitialized" in Draw()
            Vector2 startOffset = new Vector2(50, 50);

            for (int i = 0; i < count; i++)
            {
                // Column-Major indices
                int row = i % rows;
                int col = i / rows;

                visibleNodes[i].Position = startOffset + new Vector2(col * spacingX, row * spacingY);
            }

            if (visibleNodes.Count > 0) ScrollPos = visibleNodes[0].Position - new Vector2(50, 50);
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
                startPos.x += NodeWidth;
                startPos.y += NodeHeight / 2;

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
                end.y += NodeHeight / 2;
                Handles.DrawBezier(start, end, start + Vector2.right * 50, end + Vector2.left * 50, color, null, 2f);
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
