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
            Rect worldSelection = r; // Already in local space which maps to screen space inside Group?
            // Wait, Unit Pos is World. ScrollPos is Camera.
            // MousePos is in Local Group Rect (0,0 = top left of canvas view).
            // So: DrawPos = UnitPos - ScrollPos.
            // MousePos checks against DrawPos.
            // So SelectionRect is in "Draw Space".

            foreach (var unit in Data.Conversations)
            {
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

        private void DrawConnections(Rect visibleRect)
        {
            // Handles in Groups:
            // Handles.DrawBezier draws in screen space or current GUI space? 
            // It generally seems to respect GUI.matrix.
            // Let's assume it works relative to the Group.

            foreach (var unit in Data.Conversations)
            {
                Vector2 startPos = unit.Position - ScrollPos;
                startPos.x += NodeWidth;
                startPos.y += NodeHeight / 2;

                if (!string.IsNullOrEmpty(unit.NextID)) DrawCurve(startPos, unit.NextID, Color.white);
                if (unit.Choices != null)
                {
                    foreach (var c in unit.Choices) if (!string.IsNullOrEmpty(c.TargetID)) DrawCurve(startPos, c.TargetID, Color.cyan);
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
