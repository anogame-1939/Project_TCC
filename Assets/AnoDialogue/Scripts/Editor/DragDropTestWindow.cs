using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AnoGame.AnoDialogue.Editor
{
    public class DragDropTestWindow : EditorWindow
    {
        [MenuItem("Window/DragDropTest")]
        public static void ShowWindow()
        {
            GetWindow<DragDropTestWindow>("DD Test");
        }

        private List<TestItem> _items = new List<TestItem>();
        private Vector2 _scrollPos;
        private static TestItem _draggingItem;

        private class TestItem
        {
            public int ID;
            public string Name;
        }

        private void OnEnable()
        {
            if (_items.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    _items.Add(new TestItem { ID = i, Name = $"Item {i}" });
                }
            }
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            var evt = Event.current;

            // Handle Drag Exit/Cancellation
            if (evt.type == EventType.DragExited || evt.type == EventType.Ignore)
            {
                if (_draggingItem != null)
                {
                    _draggingItem = null;
                    DragAndDrop.PrepareStartDrag();
                    Repaint();
                }
            }

            // Clean up on MouseUp if drag didn't perform
            if (evt.type == EventType.MouseUp && _draggingItem != null)
            {
                _draggingItem = null;
                Repaint();
            }

            GUILayout.BeginVertical();
            GUILayout.Label($"Drag & Drop Zone Test (Dragging: {(_draggingItem != null ? _draggingItem.Name : "None")})", EditorStyles.boldLabel);
            GUILayout.Label("Green=Content, Line=Insertion", EditorStyles.miniLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Layout Constants
            float contentHeight = 24f; // Height of the actual item
            float gapHeight = 10f;     // Height of the gap
            float totalRowHeight = contentHeight + gapHeight;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                Rect totalRect = EditorGUILayout.GetControlRect(false, totalRowHeight);

                // Define Rects
                Rect contentRect = new Rect(totalRect.x, totalRect.y, totalRect.width, contentHeight);
                Rect gapRect = new Rect(totalRect.x, totalRect.yMax - gapHeight, totalRect.width, gapHeight);
                // DropZone covers bottom half of item + gap (Offset logic)
                Rect dropZoneRect = new Rect(totalRect.x, totalRect.y + (contentHeight * 0.5f), totalRect.width, contentHeight);
                // TopZone for first item
                Rect topZoneRect = new Rect(totalRect.x, totalRect.y, totalRect.width, contentHeight * 0.5f);

                // --- Debug Logs (Drag & Hover) ---
                if (evt.type == EventType.DragUpdated || evt.type == EventType.Repaint)
                {
                    // Only log periodically or for specific types to avoid Repaint spam flooding 
                    // But user requested "log when passing over".
                    // DragUpdated is best for this.
                    if (evt.type == EventType.DragUpdated)
                    {
                        if (contentRect.Contains(evt.mousePosition))
                        {
                            Debug.Log($"[DragHover] Item Content: {item.Name}");
                        }
                        if (dropZoneRect.Contains(evt.mousePosition))
                        {
                            Debug.Log($"[DragHover] DropZone(Bottom) of: {item.Name}");
                        }
                    }
                }

                // --- Base Visuals ---
                EditorGUI.DrawRect(gapRect, new Color(0.2f, 0.2f, 0.2f, 1f));
                EditorGUI.DrawRect(contentRect, new Color(0.1f, 0.3f, 0.1f, 1f));

                // Handle & Label
                Rect handleRect = new Rect(contentRect.x, contentRect.y, 24, contentRect.height);
                Rect labelRect = new Rect(contentRect.x + 24, contentRect.y, contentRect.width - 24, contentRect.height);
                EditorGUI.LabelField(handleRect, "≡", EditorStyles.centeredGreyMiniLabel);
                EditorGUI.LabelField(labelRect, item.Name, EditorStyles.whiteBoldLabel);

                // ============================================================
                // DRAWING LOGIC (Must happen during Repaint event basically)
                // ============================================================

                // 1. Normal Hover (No Drag)
                if (_draggingItem == null)
                {
                    if (contentRect.Contains(evt.mousePosition))
                        EditorGUI.DrawRect(contentRect, new Color(0f, 1f, 1f, 0.3f)); // Cyan
                }
                // 2. Dragging Hover (No background highlight, only line)
                else if (_draggingItem != null)
                {
                    // Check hit for VISUALS (Repaint)
                    bool isInDropZone = dropZoneRect.Contains(evt.mousePosition);
                    bool isInTopZone = (i == 0) && topZoneRect.Contains(evt.mousePosition);

                    if (isInDropZone)
                    {
                        // Cyan Line Only
                        float lineY = totalRect.y + contentHeight + (gapHeight * 0.5f);
                        EditorGUI.DrawRect(new Rect(totalRect.x, lineY - 1, totalRect.width, 2), Color.cyan);
                    }

                    if (isInTopZone)
                    {
                        EditorGUI.DrawRect(new Rect(totalRect.x, totalRect.y - 1, totalRect.width, 2), Color.cyan);
                    }
                }

                // --- DRAG START ---
                if (_draggingItem == null && evt.type == EventType.MouseDrag && handleRect.Contains(evt.mousePosition))
                {
                    Debug.Log($"[DragDropTest] Start Drag: {item.Name}");
                    _draggingItem = item;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("TestItem", item);
                    DragAndDrop.objectReferences = new UnityEngine.Object[0];
                    DragAndDrop.StartDrag(item.Name);
                    evt.Use();
                    Repaint();
                }

                // --- DRAG UPDATE/PERFORM (Logic Only) ---
                if (_draggingItem != null)
                {
                    bool isInDropZone = dropZoneRect.Contains(evt.mousePosition);
                    bool isInTopZone = (i == 0) && topZoneRect.Contains(evt.mousePosition);

                    if (isInDropZone || isInTopZone)
                    {
                        if (evt.type == EventType.DragUpdated)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            evt.Use();
                            Repaint(); // Force Repaint so the "Drawing Logic" block above updates visuals
                        }

                        if (evt.type == EventType.DragPerform)
                        {
                            Debug.Log($"[Drop] Performed on: {item.Name}");
                            DragAndDrop.AcceptDrag();
                            PerformReorder(_draggingItem, isInTopZone ? 0 : i + 1);
                            _draggingItem = null;
                            DragAndDrop.PrepareStartDrag();
                            evt.Use();
                            Repaint();
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (evt.type == EventType.MouseMove) Repaint();
        }

        private void PerformReorder(TestItem item, int targetIndex)
        {
            if (item == null) return;

            int oldIndex = _items.IndexOf(item);
            if (oldIndex < 0) return;

            // Adjust index for removal
            if (oldIndex < targetIndex) targetIndex--;

            _items.RemoveAt(oldIndex);

            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex > _items.Count) targetIndex = _items.Count;

            _items.Insert(targetIndex, item);
        }
    }
}
