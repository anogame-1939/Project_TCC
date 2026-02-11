using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventGraphView : GraphView
    {
        public Action OnGraphDataChanged;
        private EventList _data;
        private Dictionary<string, EventNodeView> _nodeMap = new Dictionary<string, EventNodeView>();

        public EventGraphView()
        {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphChanged;
        }

        public void PopulateGraph(EventList data)
        {
            _data = data;

            // Clear
            DeleteElements(graphElements);
            _nodeMap.Clear();

            if (_data == null || _data.events == null) return;

            // 1. Create Nodes
            foreach (var evt in _data.events)
            {
                var node = new EventNodeView(evt, () => OnGraphDataChanged?.Invoke());
                AddElement(node);
                _nodeMap[evt.eventId] = node;
            }

            // 2. Map Results to Nodes (for Result-based dependency)
            Dictionary<string, EventNodeView> resultToNode = new Dictionary<string, EventNodeView>();
            foreach (var evt in _data.events)
            {
                if (evt.results != null)
                {
                    foreach (var res in evt.results)
                    {
                        if (!string.IsNullOrEmpty(res))
                        {
                            resultToNode[res] = _nodeMap[evt.eventId];
                        }
                    }
                }
            }

            // 3. Create Edges
            foreach (var evt in _data.events)
            {
                if (evt.conditions == null) continue;

                if (!_nodeMap.ContainsKey(evt.eventId)) continue;

                var targetNode = _nodeMap[evt.eventId];

                foreach (var cond in evt.conditions)
                {
                    EventNodeView sourceNode = null;

                    // Check if condition is an Event ID
                    if (_nodeMap.ContainsKey(cond))
                    {
                        sourceNode = _nodeMap[cond];
                    }
                    // Check if condition is a Result
                    else if (resultToNode.ContainsKey(cond))
                    {
                        sourceNode = resultToNode[cond];
                    }

                    if (sourceNode != null)
                    {
                        // Check if edge already exists? GraphView allows multiple edges between ports?
                        // Default edge allows 1 edge between specific ports?
                        // ConnectTo handles creation.
                        var edge = sourceNode.OutputPort.ConnectTo(targetNode.InputPort);
                        AddElement(edge);
                    }
                }
            }

            // Auto Layout initially
            if (_data.events.Count > 0)
            {
                schedule.Execute(() => AutoLayout());
            }
        }

        private GraphViewChange OnGraphChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var sourceNode = edge.output.node as EventNodeView;
                    var targetNode = edge.input.node as EventNodeView;

                    if (sourceNode != null && targetNode != null)
                    {
                        if (targetNode.Data.conditions == null) targetNode.Data.conditions = new List<string>();

                        // Default to ID dependency
                        if (!targetNode.Data.conditions.Contains(sourceNode.Data.eventId))
                        {
                            targetNode.Data.conditions.Add(sourceNode.Data.eventId);
                            OnGraphDataChanged?.Invoke();
                        }
                    }
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is Edge edge)
                    {
                        var sourceNode = edge.output.node as EventNodeView;
                        var targetNode = edge.input.node as EventNodeView;

                        if (sourceNode != null && targetNode != null)
                        {
                            if (targetNode.Data.conditions != null)
                            {
                                // Remove ID if present
                                targetNode.Data.conditions.Remove(sourceNode.Data.eventId);

                                // Also check for Results from source
                                if (sourceNode.Data.results != null)
                                {
                                    foreach (var res in sourceNode.Data.results)
                                    {
                                        targetNode.Data.conditions.Remove(res);
                                    }
                                }
                                OnGraphDataChanged?.Invoke();
                            }
                        }
                    }

                    if (elem is EventNodeView nodeView)
                    {
                        if (_data != null && _data.events != null)
                        {
                            _data.events.Remove(nodeView.Data);
                            OnGraphDataChanged?.Invoke();
                        }
                    }
                }
            }

            return change;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            foreach (var port in ports.ToList())
            {
                if (startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatible.Add(port);
                }
            }
            return compatible;
        }

        public void AutoLayout()
        {
            if (_data == null || _data.events == null) return;

            Dictionary<string, int> depths = new Dictionary<string, int>();
            foreach (var evt in _data.events) depths[evt.eventId] = 0;

            var nodeMap = _data.events.ToDictionary(e => e.eventId);
            var resultToNodeId = new Dictionary<string, string>();
            foreach (var evt in _data.events)
            {
                if (evt.results != null)
                {
                    foreach (var r in evt.results)
                    {
                        if (!string.IsNullOrEmpty(r)) resultToNodeId[r] = evt.eventId;
                    }
                }
            }

            // Iterative depth calculation
            for (int i = 0; i < _data.events.Count + 2; i++)
            {
                bool changed = false;
                foreach (var evt in _data.events)
                {
                    int currentMax = -1;
                    if (evt.conditions != null)
                    {
                        foreach (var c in evt.conditions)
                        {
                            string parentId = null;
                            if (nodeMap.ContainsKey(c)) parentId = c;
                            else if (resultToNodeId.ContainsKey(c)) parentId = resultToNodeId[c];

                            if (parentId != null && depths.ContainsKey(parentId))
                            {
                                if (depths[parentId] > currentMax) currentMax = depths[parentId];
                            }
                        }
                    }

                    int newDepth = currentMax + 1;
                    if (newDepth > depths[evt.eventId])
                    {
                        depths[evt.eventId] = newDepth;
                        changed = true;
                    }
                }
                if (!changed) break;
            }

            var grouped = _data.events.GroupBy(e => depths[e.eventId]).OrderBy(g => g.Key);

            float xSpacing = 350f;
            float ySpacing = 250f;
            float startX = 50f;
            float startY = 50f;

            foreach (var group in grouped)
            {
                int depth = group.Key;
                int row = 0;
                foreach (var evt in group)
                {
                    if (_nodeMap.ContainsKey(evt.eventId))
                    {
                        var node = _nodeMap[evt.eventId];
                        // node.GetPosition() returns Rect
                        Rect r = node.GetPosition();
                        r.x = startX + depth * xSpacing;
                        r.y = startY + row * ySpacing;
                        node.SetPosition(r);
                    }
                    row++;
                }
            }
        }
    }
}
