using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventGraphViewV2 : GraphView
    {
        public Action OnGraphDataChanged;
        private EventList _data;
        private Dictionary<string, EventNodeView> _nodeMap = new Dictionary<string, EventNodeView>();

        public EventGraphViewV2()
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
                // Reusing EventNodeView from V1 for now as it's generic enough
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

            var allNodes = _nodeMap.Values.ToList();
            var connectedNodes = new HashSet<EventNodeView>();
            var unconnectedNodes = new List<EventNodeView>();

            // Inputs/Outputs helper
            Dictionary<EventNodeView, List<EventNodeView>> inputs = new Dictionary<EventNodeView, List<EventNodeView>>();
            Dictionary<EventNodeView, List<EventNodeView>> outputs = new Dictionary<EventNodeView, List<EventNodeView>>();

            foreach (var node in allNodes)
            {
                inputs[node] = new List<EventNodeView>();
                outputs[node] = new List<EventNodeView>();
            }

            foreach (var node in allNodes)
            {
                if (node.Data.conditions != null)
                {
                    foreach (var condId in node.Data.conditions)
                    {
                        if (_nodeMap.TryGetValue(condId, out var inputNode))
                        {
                            outputs[inputNode].Add(node);
                            inputs[node].Add(inputNode);
                        }
                        else
                        {
                            var sourceNode = allNodes.FirstOrDefault(n => n.Data.results != null && n.Data.results.Contains(condId));
                            if (sourceNode != null)
                            {
                                outputs[sourceNode].Add(node);
                                inputs[node].Add(sourceNode);
                            }
                        }
                    }
                }
            }

            foreach (var node in allNodes)
            {
                if (inputs[node].Count > 0 || outputs[node].Count > 0)
                {
                    connectedNodes.Add(node);
                }
                else
                {
                    unconnectedNodes.Add(node);
                }
            }

            // --- Layout Connected Nodes (Pyramid: Goal on Right) ---
            if (connectedNodes.Count > 0)
            {
                var goals = connectedNodes.Where(n => outputs[n].Count == 0).ToList();
                Dictionary<EventNodeView, int> ranks = new Dictionary<EventNodeView, int>();
                foreach (var node in connectedNodes) ranks[node] = -1;

                Queue<EventNodeView> queue = new Queue<EventNodeView>();
                foreach (var g in goals)
                {
                    ranks[g] = 0;
                    queue.Enqueue(g);
                }

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    int currentRank = ranks[current];

                    foreach (var input in inputs[current])
                    {
                        if (!connectedNodes.Contains(input)) continue;
                        int newRank = currentRank + 1;
                        if (newRank > ranks[input])
                        {
                            ranks[input] = newRank;
                            queue.Enqueue(input);
                        }
                    }
                }

                int maxRank = 0;
                if (ranks.Values.Any(r => r > 0)) maxRank = ranks.Values.Max();

                float xSpacing = 400f;
                float ySpacing = 250f;
                float startX = 1000f;
                float startY = 100f;

                Dictionary<EventNodeView, float> yPositions = new Dictionary<EventNodeView, float>();

                for (int r = 0; r <= maxRank; r++)
                {
                    var nodesInRank = connectedNodes.Where(n => ranks[n] == r).OrderBy(n => int.TryParse(n.Data.eventId, out int id) ? id : 0).ToList();

                    if (r == 0)
                    {
                        float currentY = startY;
                        foreach (var node in nodesInRank)
                        {
                            yPositions[node] = currentY;
                            currentY += ySpacing;
                        }
                    }
                    else
                    {
                        foreach (var node in nodesInRank)
                        {
                            var connectedOutputs = outputs[node].Where(o => ranks[o] < r).ToList();
                            if (connectedOutputs.Count > 0)
                            {
                                float avgY = connectedOutputs.Average(o => yPositions.ContainsKey(o) ? yPositions[o] : startY);
                                yPositions[node] = avgY;
                            }
                            else
                            {
                                yPositions[node] = startY + (nodesInRank.IndexOf(node)) * ySpacing;
                            }
                        }

                        nodesInRank = nodesInRank.OrderBy(n => yPositions[n]).ToList();
                        for (int i = 0; i < nodesInRank.Count - 1; i++)
                        {
                            var n1 = nodesInRank[i];
                            var n2 = nodesInRank[i + 1];
                            if (yPositions[n2] - yPositions[n1] < ySpacing)
                            {
                                yPositions[n2] = yPositions[n1] + ySpacing;
                            }
                        }
                    }

                    foreach (var node in nodesInRank)
                    {
                        float x = startX - (r * xSpacing);
                        float y = yPositions[node];
                        node.SetPosition(new Rect(x, y, 0, 0));
                    }
                }
            }

            // --- Layout Unconnected Nodes ---
            if (unconnectedNodes.Count > 0)
            {
                unconnectedNodes.Sort((a, b) =>
                {
                    int idA = int.TryParse(a.Data.eventId, out int va) ? va : 0;
                    int idB = int.TryParse(b.Data.eventId, out int vb) ? vb : 0;
                    return idA.CompareTo(idB);
                });

                float maxConnectedY = 0f;
                foreach (var n in connectedNodes)
                {
                    if (n.GetPosition().y > maxConnectedY) maxConnectedY = n.GetPosition().y;
                }

                float gridStartY = (connectedNodes.Count > 0) ? maxConnectedY + 400f : 100f;
                float gridStartX = 50f;
                float gridXSpacing = 350f;
                float gridYSpacing = 250f;
                int columns = 5;

                for (int i = 0; i < unconnectedNodes.Count; i++)
                {
                    int row = i / columns;
                    int col = i % columns;

                    float x = gridStartX + col * gridXSpacing;
                    float y = gridStartY + row * gridYSpacing;

                    unconnectedNodes[i].SetPosition(new Rect(x, y, 0, 0));
                }
            }
        }
    }
}
