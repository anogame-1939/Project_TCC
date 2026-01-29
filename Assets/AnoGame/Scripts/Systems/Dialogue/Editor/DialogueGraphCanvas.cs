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
        private bool _isDraggingSelectionBox = false;
        private Vector2 _selectionStartPos;
        private Rect _selectionRect;

        // Caching
        private Dictionary<string, Vector2> _posCache = new Dictionary<string, Vector2>();

        public DialogueGraphCanvas(MasterDialogueData data, EditorWindow host)
        {
            Data = data;
            _host = host;
        }

        public void Draw(Rect position)
        {
            if (Data == null) return;

            // Clip to the canvas area using a Group just for the background/grid, 
            // BUT for Windows we need to be careful. 
            // Actually, typical node editors in Unity IMGUI just draw everything and let the EditorWindow clip it.
            // Since we have a sidebar, we want to start drawing to the right.

            // Define the canvas area for input processing
            GUI.BeginGroup(position);
            Rect localRect = new Rect(0, 0, position.width, position.height);

            // Draw Background & Grid
            DrawGrid(localRect, 20, 0.2f, Color.gray);
            DrawGrid(localRect, 100, 0.4f, Color.gray);

            // Process Input relative to this group
            ProcessEvents(Event.current, localRect);

            // 1. Draw Connections (Behind nodes)
            UpdatePosCache();
            if (Event.current.type == EventType.Repaint)
            {
                DrawConnections(localRect);
            }

            // Draw Selection Box
            if (_isDraggingSelectionBox)
            {
                GUI.Box(_selectionRect, "", "SelectionRect");
            }

            GUI.EndGroup();

            // 2. Draw Nodes (Windows)
            // GUILayout.Window is top-level, so we must offset the rects manually by the Sidebar width + Scroll
            // 'position' is the screen rect of the canvas area.

            BeginWindows();
            for (int i = 0; i < Data.Conversations.Count; i++)
            {
                var unit = Data.Conversations[i];

                // Init pos
                if (unit.Position == Vector2.zero) unit.Position = new Vector2(100 + (i * 20), 100 + (i * 20));

                // Calculate Window Rect in Screen Space (relative to EditorWindow)
                float nodeX = position.x + (unit.Position.x - ScrollPos.x);
                float nodeY = position.y + (unit.Position.y - ScrollPos.y);

                Rect nodeRect = new Rect(nodeX, nodeY, NodeWidth, NodeHeight);

                // Culling: Don't draw if completely out of view
                bool isVisible = (nodeRect.xMax >= position.x && nodeRect.x <= position.xMax && nodeRect.yMax >= position.y && nodeRect.y <= position.yMax);

                // Still draw windows that are off-screen to keep IDs consistent if possible, 
                // but for performance we can skip if many. 
                // However, IMGUI window IDs are based on order. Skipping changes order.
                // We must draw all windows or manage IDs manually. Rely on standard behavior for now.

                // Highlight
                if (_selectedIDs.Contains(unit.ID))
                {
                    EditorGUI.DrawRect(new Rect(nodeRect.x - 2, nodeRect.y - 2, nodeRect.width + 4, nodeRect.height + 4), Color.cyan);
                }

                Rect newRect = GUILayout.Window(i, nodeRect, (id) => DrawNodeWindow(id), unit.ID);

                // Handle Movement
                // Since we manually offset by position.x/y and scroll, we need to reverse that to get the new Unit Position
                Vector2 newPosScreen = new Vector2(newRect.x, newRect.y);
                Vector2 newPosLocal = newPosScreen - new Vector2(position.x, position.y) + ScrollPos;

                if (newPosLocal != unit.Position)
                {
                    // Detect delta
                    Vector2 delta = newPosLocal - unit.Position;
                    unit.Position = newPosLocal;

                    // If this node is selected, move others too!
                    if (_selectedIDs.Contains(unit.ID))
                    {
                        MoveSelectedNodes(delta, unit.ID); // Exclude self as it's already moved
                    }
                }
            }
            EndWindows();
        }

        public void DrawNodeWindow(int id)
        {
            if (id < 0 || id >= Data.Conversations.Count) return;
            var unit = Data.Conversations[id];

            // Event capturing for selection (Clicking window selects it)
            if (Event.current.type == EventType.MouseDown)
            {
                if (!Event.current.shift && !Event.current.control && !_selectedIDs.Contains(unit.ID))
                {
                    _selectedIDs.Clear();
                }
                _selectedIDs.Add(unit.ID);
                GUI.changed = true;
            }

            EditorGUILayout.BeginVertical();

            // Content
            EditorGUILayout.LabelField("Speaker:", EditorStyles.miniLabel);
            unit.SpeakerName = EditorGUILayout.TextField(unit.SpeakerName);

            EditorGUILayout.LabelField("Text:", EditorStyles.miniLabel);
            unit.BodyText = EditorGUILayout.TextArea(unit.BodyText, GUILayout.Height(40));

            // Links (simplified view for now, could be enhanced)
            if (unit.Choices != null && unit.Choices.Count > 0)
            {
                EditorGUILayout.LabelField("Choices:", EditorStyles.miniLabel);
                foreach (var c in unit.Choices)
                {
                    EditorGUILayout.LabelField($"-> {c.TargetID}");
                }
            }
            else if (!string.IsNullOrEmpty(unit.NextID))
            {
                EditorGUILayout.LabelField($"-> {unit.NextID}");
            }

            EditorGUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void BeginWindows()
        {
            if (_host != null) _host.BeginWindows();
        }

        private void EndWindows()
        {
            if (_host != null) _host.EndWindows();
        }

        private void MoveSelectedNodes(Vector2 delta, string excludeID)
        {
            foreach (var unit in Data.Conversations)
            {
                if (_selectedIDs.Contains(unit.ID) && unit.ID != excludeID)
                {
                    unit.Position += delta;
                }
            }
        }

        private void ProcessEvents(Event e, Rect viewRect)
        {
            // Panning (Middle Mouse or Alt+Left)
            if (e.type == EventType.MouseDrag && e.button == 2 && viewRect.Contains(e.mousePosition))
            {
                ScrollPos -= e.delta;
                GUI.changed = true;
                e.Use();
            }

            // Box Selection (Left Mouse Drag on Empty Space)
            if (e.type == EventType.MouseDown && e.button == 0 && viewRect.Contains(e.mousePosition))
            {
                // We assume if we are here, we missed windows (handled by DrawNodeWindow logic hopefully, or we check rects)
                _isDraggingSelectionBox = true;
                _selectionStartPos = e.mousePosition;
                if (!e.shift && !e.control) _selectedIDs.Clear();
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDraggingSelectionBox)
            {
                _selectionRect = new Rect(
                    Mathf.Min(_selectionStartPos.x, e.mousePosition.x),
                    Mathf.Min(_selectionStartPos.y, e.mousePosition.y),
                    Mathf.Abs(e.mousePosition.x - _selectionStartPos.x),
                    Mathf.Abs(e.mousePosition.y - _selectionStartPos.y)
                );
                GUI.changed = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _isDraggingSelectionBox)
            {
                _isDraggingSelectionBox = false;
                SelectNodesInRect(_selectionRect, viewRect);
                e.Use();
            }
        }

        private void SelectNodesInRect(Rect r, Rect viewOffset)
        {
            // r is in Local Group space.
            Rect worldSelection = r;
            worldSelection.x += ScrollPos.x;
            worldSelection.y += ScrollPos.y;

            foreach (var unit in Data.Conversations)
            {
                Rect nodeRect = new Rect(unit.Position.x, unit.Position.y, NodeWidth, NodeHeight);
                if (worldSelection.Overlaps(nodeRect))
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
            // Drawing lines using Handles in a GUI Group.
            // (0,0) is top-left of canvas.
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
