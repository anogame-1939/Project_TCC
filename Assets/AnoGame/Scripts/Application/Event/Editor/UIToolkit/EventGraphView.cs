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

            // Identify connected vs unconnected nodes
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

            // Build graph structure from edges/connections
            // Direction: Input -> Output (Predecessor -> Successor)
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
                        // Check result-based connections
                        else
                        {
                            // If condition is a result ID, find the node that produces it
                            // Need reverse lookup for results
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

            // --- Layout Connected Nodes (Pyramid/Tree: Goal on Right) ---
            if (connectedNodes.Count > 0)
            {
                // Identify Goals (nodes with no outputs within the connected set)
                var goals = connectedNodes.Where(n => outputs[n].Count == 0).ToList();

                // Calculate Rank (distance from Goal)
                // Goal = Rank 0
                // Inputs to Goal = Rank 1, etc.
                Dictionary<EventNodeView, int> ranks = new Dictionary<EventNodeView, int>();
                foreach (var node in connectedNodes) ranks[node] = -1; // -1 = unvisited

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
                        // Ensure input is part of connected set (it should be)
                        if (!connectedNodes.Contains(input)) continue;

                        // Assign rank: max(existing, current + 1)
                        int newRank = currentRank + 1;
                        if (newRank > ranks[input])
                        {
                            ranks[input] = newRank;
                            queue.Enqueue(input); // Re-queue to propagate deeper ranks if needed
                        }
                    }
                }

                // Handle cycles or disconnected sub-graphs that weren't reached (shouldn't happen if logic is correct for "connected")
                // If any node has rank -1, treat it as rank (max_rank + 1) or separate group
                int maxRank = 0;
                if (ranks.Values.Any(r => r > 0))
                {
                    maxRank = ranks.Values.Max();
                }

                // Layout Parameters
                float xSpacing = 400f; // Horizontal spacing connects are longer
                float ySpacing = 250f;
                float startX = 1000f; // Goal starts here
                float startY = 100f; // Center Y?

                // Group by Rank
                var rankGroups = connectedNodes.GroupBy(n => ranks[n]).OrderBy(g => g.Key);

                // Y-positioning strategy:
                // Simple approach: Center each "layer" vertically relative to layout center
                // Better approach: Calculate desired Y based on children's Y (barycenter)
                // But we are traversing Right-to-Left (Goal to Start).
                // Let's position Goals first.
                // Then inputs.
                Dictionary<EventNodeView, float> yPositions = new Dictionary<EventNodeView, float>();

                // Sort ranks 0..Max
                for (int r = 0; r <= maxRank; r++)
                {
                    var nodesInRank = connectedNodes.Where(n => ranks[n] == r).OrderBy(n => int.TryParse(n.Data.eventId, out int id) ? id : 0).ToList();

                    if (r == 0) // Goals
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
                        // Position based on outputs (which are at rank r-1, or < r)
                        foreach (var node in nodesInRank)
                        {
                            var connectedOutputs = outputs[node].Where(o => ranks[o] < r).ToList();
                            if (connectedOutputs.Count > 0)
                            {
                                // Average Y of outputs
                                float avgY = connectedOutputs.Average(o => yPositions.ContainsKey(o) ? yPositions[o] : startY);
                                yPositions[node] = avgY;
                            }
                            else
                            {
                                // Fallback (shouldn't happen for connected nodes unless cycle issues)
                                yPositions[node] = startY + (nodesInRank.IndexOf(node)) * ySpacing;
                            }
                        }

                        // Collision resolution: prevent overlap in same rank
                        nodesInRank = nodesInRank.OrderBy(n => yPositions[n]).ToList();
                        for (int i = 0; i < nodesInRank.Count - 1; i++)
                        {
                            var n1 = nodesInRank[i];
                            var n2 = nodesInRank[i + 1];
                            if (yPositions[n2] - yPositions[n1] < ySpacing)
                            {
                                // Shift n2 down
                                yPositions[n2] = yPositions[n1] + ySpacing;
                            }
                        }
                    }

                    // Apply Positions
                    foreach (var node in nodesInRank)
                    {
                        float x = startX - (r * xSpacing);
                        float y = yPositions[node];
                        node.SetPosition(new Rect(x, y, 0, 0));
                    }
                }
            }

            // --- Layout Unconnected Nodes (Grid) ---
            if (unconnectedNodes.Count > 0)
            {
                unconnectedNodes.Sort((a, b) =>
                {
                    int idA = int.TryParse(a.Data.eventId, out int va) ? va : 0;
                    int idB = int.TryParse(b.Data.eventId, out int vb) ? vb : 0;
                    return idA.CompareTo(idB);
                });

                float gridStartX = 50f;
                // Place grid below the connected graph or at a fixed Y
                // Find max Y of connected graph to place below?
                // Or just place at fixed Y=1000 if graph is small?
                // Let's calculate bounds of connected graph
                float maxConnectedY = 0f;
                foreach (var n in connectedNodes)
                {
                    if (n.GetPosition().y > maxConnectedY) maxConnectedY = n.GetPosition().y;
                }

                float gridStartY = (connectedNodes.Count > 0) ? maxConnectedY + 400f : 100f;
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
