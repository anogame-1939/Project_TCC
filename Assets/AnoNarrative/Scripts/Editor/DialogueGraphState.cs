using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.AnoNarrative.Editor
{
    public class DialogueGraphState
    {
        // View State
        public Vector2 ScrollPos;
        public float Zoom = 1.0f;

        // Selection
        public readonly HashSet<string> SelectedIDs = new HashSet<string>();

        // Dragging
        public bool IsDraggingNode { get; private set; }
        public string DraggingNodeID { get; private set; }
        public bool IsDraggingSelectionBox { get; private set; }
        public Vector2 SelectionStartPos { get; private set; }
        public Rect SelectionRect { get; private set; }

        // Interaction
        public Vector2 LastMousePos;

        // Methods
        public void SetSelection(string id)
        {
            SelectedIDs.Clear();
            if (!string.IsNullOrEmpty(id)) SelectedIDs.Add(id);
        }

        public void AddToSelection(string id)
        {
            if (!string.IsNullOrEmpty(id)) SelectedIDs.Add(id);
        }

        public void RemoveFromSelection(string id)
        {
            if (!string.IsNullOrEmpty(id)) SelectedIDs.Remove(id);
        }

        public void ClearSelection()
        {
            SelectedIDs.Clear();
        }

        public bool IsSelected(string id)
        {
            return SelectedIDs.Contains(id);
        }

        public void StartDraggingNode(string id, Vector2 mousePos)
        {
            IsDraggingNode = true;
            DraggingNodeID = id;
            LastMousePos = mousePos;
        }

        public void StopDraggingNode()
        {
            IsDraggingNode = false;
            DraggingNodeID = null;
        }

        public void StartDraggingSelectionBox(Vector2 mousePos)
        {
            IsDraggingSelectionBox = true;
            SelectionStartPos = mousePos;
            SelectionRect = new Rect(mousePos, Vector2.zero);
        }

        public void UpdateSelectionBox(Vector2 mousePos)
        {
            SelectionRect = Rect.MinMaxRect(
                Mathf.Min(SelectionStartPos.x, mousePos.x),
                Mathf.Min(SelectionStartPos.y, mousePos.y),
                Mathf.Max(SelectionStartPos.x, mousePos.x),
                Mathf.Max(SelectionStartPos.y, mousePos.y)
            );
        }

        public void StopDraggingSelectionBox()
        {
            IsDraggingSelectionBox = false;
        }
    }
}
