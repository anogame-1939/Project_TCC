#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventGraphViewV2 : GraphView
    {
        public Action OnGraphDataChanged;
        private List<EventData> _eventDataList;
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
        }

        public void PopulateGraph(List<EventData> eventDataList, HashSet<string> knownItemIds)
        {
            _eventDataList = eventDataList;

            // Clear
            DeleteElements(graphElements);
            _nodeMap.Clear();

            if (_eventDataList == null || _eventDataList.Count == 0) return;

            // Build known event ID set for validation
            var knownEventIds = new HashSet<string>();
            foreach (var ed in _eventDataList)
            {
                knownEventIds.Add(ed.EventId);
            }

            // Also add all results as "known" (they are produced by events)
            var allResults = new HashSet<string>();
            foreach (var ed in _eventDataList)
            {
                if (ed.Results != null)
                {
                    foreach (var res in ed.Results)
                    {
                        if (!string.IsNullOrEmpty(res))
                        {
                            allResults.Add(res);
                            knownEventIds.Add(res); // Results are "known" for condition validation
                        }
                    }
                }
            }

            // Also add known item IDs to the validation set
            var combinedKnownIds = new HashSet<string>(knownEventIds);
            if (knownItemIds != null)
            {
                foreach (var id in knownItemIds)
                {
                    combinedKnownIds.Add(id);
                }
            }

            // 1. Create Nodes
            foreach (var ed in _eventDataList)
            {
                var node = new EventNodeView(ed, combinedKnownIds, knownItemIds ?? new HashSet<string>());
                AddElement(node);
                _nodeMap[ed.EventId] = node;
            }

            // 2. Map Results to Nodes (for result-based dependency)
            var resultToNode = new Dictionary<string, EventNodeView>();
            foreach (var ed in _eventDataList)
            {
                if (ed.Results != null)
                {
                    foreach (var res in ed.Results)
                    {
                        if (!string.IsNullOrEmpty(res))
                        {
                            resultToNode[res] = _nodeMap[ed.EventId];
                        }
                    }
                }
            }

            // 3. Create Edges from RequiredEventIds
            foreach (var ed in _eventDataList)
            {
                if (!_nodeMap.ContainsKey(ed.EventId)) continue;
                var targetNode = _nodeMap[ed.EventId];

                // Event conditions
                var reqEvents = ed.RequiredEventIds;
                if (reqEvents != null)
                {
                    foreach (var reqId in reqEvents)
                    {
                        if (_nodeMap.TryGetValue(reqId, out var sourceNode))
                        {
                            var edge = sourceNode.OutputPort.ConnectTo(targetNode.InputPort);
                            AddElement(edge);
                        }
                    }
                }

                // Item conditions — find the event that produces this item (via results)
                var reqItems = ed.RequiredItemIds;
                if (reqItems != null)
                {
                    foreach (var itemId in reqItems)
                    {
                        if (resultToNode.TryGetValue(itemId, out var sourceNode))
                        {
                            var edge = sourceNode.OutputPort.ConnectTo(targetNode.InputPort);
                            AddElement(edge);
                        }
                    }
                }
            }

            // Restore positions from meta or auto layout
            var meta = EventGraphMeta.Load();
            bool hasPositions = meta.nodePositions != null && meta.nodePositions.Count > 0;
            bool restored = false;

            if (hasPositions)
            {
                int restoredCount = 0;
                foreach (var kvp in _nodeMap)
                {
                    var pos = meta.GetNodePosition(kvp.Key);
                    if (pos.HasValue)
                    {
                        kvp.Value.SetPosition(new Rect(pos.Value.x, pos.Value.y, 0, 0));
                        restoredCount++;
                    }
                }
                restored = restoredCount > 0;
            }

            if (!restored && _eventDataList.Count > 0)
            {
                schedule.Execute(() => AutoLayout());
            }
        }

        /// <summary>
        /// Save all current node positions to the meta file.
        /// </summary>
        public void SaveNodePositions()
        {
            var meta = EventGraphMeta.Load();
            foreach (var kvp in _nodeMap)
            {
                var rect = kvp.Value.GetPosition();
                meta.SetNodePosition(kvp.Key, new Vector2(rect.x, rect.y));
            }
            meta.Save();
            Debug.Log("Event Graph V2: Node positions saved.");
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
            if (_eventDataList == null || _eventDataList.Count == 0) return;

            var allNodes = _nodeMap.Values.ToList();
            var connectedNodes = new HashSet<EventNodeView>();
            var unconnectedNodes = new List<EventNodeView>();

            // Build adjacency from edges
            var inputs = new Dictionary<EventNodeView, List<EventNodeView>>();
            var outputs = new Dictionary<EventNodeView, List<EventNodeView>>();

            foreach (var node in allNodes)
            {
                inputs[node] = new List<EventNodeView>();
                outputs[node] = new List<EventNodeView>();
            }

            // Walk edges to build adjacency
            foreach (var edge in edges.ToList())
            {
                var sourceNode = edge.output.node as EventNodeView;
                var targetNode = edge.input.node as EventNodeView;
                if (sourceNode != null && targetNode != null)
                {
                    outputs[sourceNode].Add(targetNode);
                    inputs[targetNode].Add(sourceNode);
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
                var ranks = new Dictionary<EventNodeView, int>();
                foreach (var node in connectedNodes) ranks[node] = -1;

                var queue = new Queue<EventNodeView>();
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

                var yPositions = new Dictionary<EventNodeView, float>();

                for (int r = 0; r <= maxRank; r++)
                {
                    var nodesInRank = connectedNodes
                        .Where(n => ranks[n] == r)
                        .OrderBy(n => n.EventData.EventId)
                        .ToList();

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
                            var connectedOutputs = outputs[node].Where(o => ranks.ContainsKey(o) && ranks[o] < r).ToList();
                            if (connectedOutputs.Count > 0)
                            {
                                float avgY = connectedOutputs.Average(o => yPositions.ContainsKey(o) ? yPositions[o] : startY);
                                yPositions[node] = avgY;
                            }
                            else
                            {
                                yPositions[node] = startY + nodesInRank.IndexOf(node) * ySpacing;
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
                unconnectedNodes.Sort((a, b) => string.Compare(a.EventData.EventId, b.EventData.EventId, StringComparison.Ordinal));

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
#endif
