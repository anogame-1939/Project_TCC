using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoNarrative.Editor
{
    /// <summary>
    /// GraphView-based canvas for the Dialogue Graph, replacing the IMGUI DialogueGraphCanvas.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        public MasterDialogueData Data { get; private set; }

        // Filter (int.MinValue means All)
        public int FilterEpisode = int.MinValue;
        public int FilterChapter = int.MinValue;
        public int FilterSection = int.MinValue;
        public string FilterSectionName = null;

        // Overlay labels
        private Label _filterOverlayLabel;
        private Label _debugOverlayLabel;

        // Node map
        private readonly Dictionary<string, DialogueNodeView> _nodeViewMap = new Dictionary<string, DialogueNodeView>();

        // Events
        public Action OnGraphDataChanged;

        /// <summary>Whether to use Manhattan (right-angle) edges or default Bezier edges.</summary>
        public bool UseManhattanEdges { get; set; } = false;

        public DialogueGraphView(MasterDialogueData data)
        {
            Data = data;

            // Standard manipulators
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Grid background
            var gridBg = new GridBackground();
            Insert(0, gridBg);
            gridBg.StretchToParentSize();

            // Overlay labels
            _filterOverlayLabel = new Label();
            _filterOverlayLabel.AddToClassList("filter-overlay-label");
            _filterOverlayLabel.pickingMode = PickingMode.Ignore;
            Add(_filterOverlayLabel);

            _debugOverlayLabel = new Label();
            _debugOverlayLabel.AddToClassList("debug-overlay-label");
            _debugOverlayLabel.pickingMode = PickingMode.Ignore;
            _debugOverlayLabel.pickingMode = PickingMode.Ignore;
            Add(_debugOverlayLabel);

            // Reorder layers to ensure connections are drawn on top of nodes
            schedule.Execute(() =>
            {
                // Typically GraphView has: Grid(0), Connections(1), Nodes(2)
                // We want: Grid(0), Nodes(1), Connections(2)
                var connectionsLayer = this.Q("connections");
                connectionsLayer?.BringToFront();
            });

            // Edge connection listener
            graphViewChanged = OnGraphViewChanged;

            // Style - loaded from window via USS file

            if (data != null)
            {
                PopulateGraph();
            }
        }

        /// <summary>
        /// Set filter and rebuild visible nodes.
        /// </summary>
        public void SetFilter(int ep, int ch, int sec, string secName = null)
        {
            FilterEpisode = ep;
            FilterChapter = ch;
            FilterSection = sec;
            FilterSectionName = secName;

            PopulateGraph();

            // Pan to first node
            var firstNode = Data.Conversations.FirstOrDefault(u => IsUnitVisible(u));
            if (firstNode != null)
            {
                FrameNode(firstNode.ID);
            }

            UpdateOverlayLabel();
        }

        /// <summary>
        /// Rebuild the entire graph from data.
        /// </summary>
        public void PopulateGraph()
        {
            // Clear existing
            foreach (var node in _nodeViewMap.Values)
            {
                RemoveElement(node);
            }
            _nodeViewMap.Clear();

            // Clear edges
            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            if (Data == null) return;

            // Create nodes
            foreach (var unit in Data.Conversations)
            {
                if (!IsUnitVisible(unit)) continue;

                var nodeView = new DialogueNodeView(unit, Data, () =>
                {
                    MarkDataDirty();
                });
                AddElement(nodeView);
                _nodeViewMap[unit.ID] = nodeView;
            }

            // Create edges
            foreach (var unit in Data.Conversations)
            {
                if (!IsUnitVisible(unit)) continue;
                if (!_nodeViewMap.ContainsKey(unit.ID)) continue;

                var sourceView = _nodeViewMap[unit.ID];

                // NextID edge
                if (!string.IsNullOrEmpty(unit.NextID) && _nodeViewMap.ContainsKey(unit.NextID))
                {
                    var targetView = _nodeViewMap[unit.NextID];
                    var edge = CreateEdge(sourceView.OutputPort, targetView.InputPort);
                    edge.output.Connect(edge);
                    edge.input.Connect(edge);
                    AddElement(edge);
                }

                // Choice edges
                if (unit.Choices != null)
                {
                    for (int i = 0; i < unit.Choices.Count && i < sourceView.ChoicePorts.Count; i++)
                    {
                        var choice = unit.Choices[i];
                        if (!string.IsNullOrEmpty(choice.TargetID) && _nodeViewMap.ContainsKey(choice.TargetID))
                        {
                            var targetView = _nodeViewMap[choice.TargetID];
                            var edge = CreateEdge(sourceView.ChoicePorts[i], targetView.InputPort);
                            edge.output.Connect(edge);
                            edge.input.Connect(edge);
                            AddElement(edge);
                        }
                    }
                }
            }

            UpdateOverlayLabel();

            // Ensure connections layer is drawn on top of nodes
            schedule.Execute(() => BringEdgeLayerToFront());
        }

        /// <summary>
        /// Center view on a specific node.
        /// </summary>
        public void FrameNode(string nodeID)
        {
            if (_nodeViewMap.TryGetValue(nodeID, out var nodeView))
            {
                // Use schedule to wait for layout
                schedule.Execute(() =>
                {
                    var nodeRect = nodeView.GetPosition();
                    var graphRect = contentRect;

                    var targetPos = new Vector3(
                        -(nodeRect.x - graphRect.width / 2 + nodeRect.width / 2),
                        -(nodeRect.y - graphRect.height / 2 + nodeRect.height / 2),
                        0
                    );

                    UpdateViewTransform(targetPos, contentViewContainer.transform.scale);
                });
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (port.direction != startPort.direction && port.node != startPort.node)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // Handle edge creation
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var outputNode = edge.output.node as DialogueNodeView;
                    var inputNode = edge.input.node as DialogueNodeView;
                    if (outputNode == null || inputNode == null) continue;

                    // Determine if this is a NextID or Choice connection
                    if (edge.output == outputNode.OutputPort)
                    {
                        outputNode.Unit.NextID = inputNode.Unit.ID;
                    }
                    else
                    {
                        int choiceIdx = outputNode.ChoicePorts.ToList().IndexOf(edge.output);
                        if (choiceIdx >= 0 && choiceIdx < outputNode.Unit.Choices.Count)
                        {
                            outputNode.Unit.Choices[choiceIdx].TargetID = inputNode.Unit.ID;
                        }
                    }
                }
                MarkDataDirty();
            }

            // Handle edge removal
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        var outputNode = edge.output?.node as DialogueNodeView;
                        if (outputNode == null) continue;

                        if (edge.output == outputNode.OutputPort)
                        {
                            outputNode.Unit.NextID = null;
                        }
                        else
                        {
                            int choiceIdx = outputNode.ChoicePorts.ToList().IndexOf(edge.output);
                            if (choiceIdx >= 0 && choiceIdx < outputNode.Unit.Choices.Count)
                            {
                                outputNode.Unit.Choices[choiceIdx].TargetID = null;
                            }
                        }
                    }

                    if (element is DialogueNodeView deletedNode)
                    {
                        // Cleanup references in data
                        foreach (var unit in Data.Conversations)
                        {
                            if (unit.NextID == deletedNode.Unit.ID) unit.NextID = null;
                            if (unit.Choices != null)
                            {
                                foreach (var c in unit.Choices)
                                {
                                    if (c.TargetID == deletedNode.Unit.ID) c.TargetID = null;
                                }
                            }
                        }

                        Data.Conversations.Remove(deletedNode.Unit);
                        _nodeViewMap.Remove(deletedNode.Unit.ID);
                    }
                }
                MarkDataDirty();
            }

            // Handle node movement
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is DialogueNodeView movedNode)
                    {
                        movedNode.SyncPositionToUnit();
                    }
                }
                MarkDataDirty();
            }

            return change;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Get position in graph space
            var localMousePos = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Add Node", action =>
            {
                CreateNode(localMousePos);
            });

            // If clicked on a node, add more options
            var targetNode = evt.target as DialogueNodeView;
            if (targetNode != null)
            {
                evt.menu.AppendAction("Insert Node After", action =>
                {
                    InsertNodeAfter(targetNode.Unit);
                });
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete Node", action =>
                {
                    DeleteNode(targetNode);
                });
            }
        }

        private void CreateNode(Vector2 position)
        {
            string newID = GenerateNextID();

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = FilterEpisode != int.MinValue ? FilterEpisode : 1,
                ChapterID = FilterChapter != int.MinValue ? FilterChapter : 1,
                SectionID = FilterSection != int.MinValue ? FilterSection : 1,
                SpeakerName = "New Speaker",
                BodyText = "New Text",
                Position = position
            };

            Data.Conversations.Add(newUnit);

            var nodeView = new DialogueNodeView(newUnit, Data, () => MarkDataDirty());
            AddElement(nodeView);
            _nodeViewMap[newID] = nodeView;

            MarkDataDirty();
        }

        private void InsertNodeAfter(ConversationUnit parent)
        {
            string newID = GenerateNextID();
            Vector2 newPos = parent.Position + new Vector2(0, 200);

            var newUnit = new ConversationUnit
            {
                ID = newID,
                EpisodeID = parent.EpisodeID,
                ChapterID = parent.ChapterID,
                SectionID = parent.SectionID,
                SpeakerName = "New Speaker",
                BodyText = "New Text",
                Position = newPos,
                NextID = parent.NextID
            };

            parent.NextID = newID;
            Data.Conversations.Add(newUnit);

            // Rebuild to show new connections
            PopulateGraph();
            MarkDataDirty();
        }

        private void DeleteNode(DialogueNodeView nodeView)
        {
            // Cleanup references
            foreach (var unit in Data.Conversations)
            {
                if (unit.NextID == nodeView.Unit.ID) unit.NextID = null;
                if (unit.Choices != null)
                {
                    foreach (var c in unit.Choices)
                    {
                        if (c.TargetID == nodeView.Unit.ID) c.TargetID = null;
                    }
                }
            }

            Data.Conversations.Remove(nodeView.Unit);
            _nodeViewMap.Remove(nodeView.Unit.ID);
            RemoveElement(nodeView);

            // Remove connected edges
            foreach (var edge in edges.ToList())
            {
                if (edge.output?.node == nodeView || edge.input?.node == nodeView)
                {
                    RemoveElement(edge);
                }
            }

            MarkDataDirty();
        }

        private string GenerateNextID()
        {
            int ep = FilterEpisode != int.MinValue ? FilterEpisode : 1;
            int ch = FilterChapter != int.MinValue ? FilterChapter : 1;
            int sec = FilterSection != int.MinValue ? FilterSection : 1;

            string prefix = $"{ep}_{ch}_{sec}_";

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

        private bool IsUnitVisible(ConversationUnit unit)
        {
            if (FilterEpisode == int.MinValue && FilterChapter == int.MinValue && FilterSection == int.MinValue) return true;

            bool matchEp = FilterEpisode == int.MinValue || unit.EpisodeID == FilterEpisode;
            bool matchCh = FilterChapter == int.MinValue || unit.ChapterID == FilterChapter;
            bool matchSec = FilterSection == int.MinValue || unit.SectionID == FilterSection;

            bool matchName = true;
            if (!string.IsNullOrEmpty(FilterSectionName))
            {
                matchName = unit.SectionName == FilterSectionName;
            }

            return matchEp && matchCh && matchSec && matchName;
        }

        private void MarkDataDirty()
        {
            if (Data != null)
            {
                EditorUtility.SetDirty(Data);
                AssetDatabase.SaveAssetIfDirty(Data);
            }
            OnGraphDataChanged?.Invoke();
        }

        /// <summary>
        /// Create an edge based on the current edge type setting.
        /// </summary>
        private Edge CreateEdge(Port outputPort, Port inputPort)
        {
            if (UseManhattanEdges)
            {
                return new ManhattanEdge
                {
                    output = outputPort,
                    input = inputPort
                };
            }
            else
            {
                return new Edge
                {
                    output = outputPort,
                    input = inputPort
                };
            }
        }

        /// <summary>
        /// Bring the edge (connections) layer to the front so edges render on top of nodes.
        /// GraphView layers are unnamed, so we find the layer containing Edge elements.
        /// </summary>
        private void BringEdgeLayerToFront()
        {
            foreach (var child in contentViewContainer.Children())
            {
                if (child.Children().Any(c => c is Edge))
                {
                    child.BringToFront();
                    break;
                }
            }
        }

        private void UpdateOverlayLabel()
        {
            if (FilterEpisode != int.MinValue || FilterChapter != int.MinValue || FilterSection != int.MinValue)
            {
                var sample = Data.Conversations.FirstOrDefault(u => IsUnitVisible(u));
                string secName = !string.IsNullOrEmpty(FilterSectionName)
                    ? FilterSectionName
                    : (sample?.SectionName ?? "All");

                string label;
                if (FilterEpisode != int.MinValue && FilterChapter != int.MinValue && FilterSection != int.MinValue)
                {
                    label = $"{FilterEpisode}.{FilterChapter}.{FilterSection}_{secName}";
                }
                else
                {
                    string epStr = FilterEpisode != int.MinValue ? FilterEpisode.ToString() : "*";
                    string chStr = FilterChapter != int.MinValue ? FilterChapter.ToString() : "*";
                    string secStr = FilterSection != int.MinValue ? FilterSection.ToString() : "*";
                    label = $"{epStr}.{chStr}.{secStr}_{secName}";
                }

                _filterOverlayLabel.text = label;
            }
            else
            {
                _filterOverlayLabel.text = "";
            }
        }

        // --- Auto Layout ---

        public void AutoLayoutFlow()
        {
            var visibleNodes = Data.Conversations.Where(IsUnitVisible).ToList();
            if (visibleNodes.Count == 0) return;

            var levels = GetNodeLevels(visibleNodes);
            var nodesByLevel = new Dictionary<int, List<ConversationUnit>>();

            foreach (var node in visibleNodes)
            {
                int lvl = levels[node.ID];
                if (!nodesByLevel.ContainsKey(lvl)) nodesByLevel[lvl] = new List<ConversationUnit>();
                nodesByLevel[lvl].Add(node);
            }

            float spacingX = 400f;
            float spacingY = 250f;
            Vector2 startOffset = new Vector2(50, 50);

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

            PopulateGraph();
            if (visibleNodes.Count > 0) FrameNode(visibleNodes[0].ID);
            MarkDataDirty();
        }

        public void AutoLayoutGrid()
        {
            var visibleNodes = Data.Conversations.Where(IsUnitVisible).ToList();
            if (visibleNodes.Count == 0) return;

            var levels = GetNodeLevels(visibleNodes);

            visibleNodes.Sort((a, b) =>
            {
                int levelA = levels.ContainsKey(a.ID) ? levels[a.ID] : 0;
                int levelB = levels.ContainsKey(b.ID) ? levels[b.ID] : 0;
                if (levelA != levelB) return levelA.CompareTo(levelB);
                return CompareNodeIDs(a, b);
            });

            int count = visibleNodes.Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count)));

            float spacingX = 400f;
            float spacingY = 250f;
            Vector2 startOffset = new Vector2(50, 50);

            for (int i = 0; i < count; i++)
            {
                int row = i % rows;
                int col = i / rows;
                visibleNodes[i].Position = startOffset + new Vector2(col * spacingX, row * spacingY);
            }

            PopulateGraph();
            if (visibleNodes.Count > 0) FrameNode(visibleNodes[0].ID);
            MarkDataDirty();
        }

        private Dictionary<string, int> GetNodeLevels(List<ConversationUnit> nodes)
        {
            var adjacency = new Dictionary<string, List<string>>();
            var inDegree = new Dictionary<string, int>();
            var nodeMap = new Dictionary<string, ConversationUnit>();

            foreach (var n in nodes)
            {
                nodeMap[n.ID] = n;
                if (!adjacency.ContainsKey(n.ID)) adjacency[n.ID] = new List<string>();
                if (!inDegree.ContainsKey(n.ID)) inDegree[n.ID] = 0;
            }

            foreach (var n in nodes)
            {
                var targets = new List<string>();
                if (!string.IsNullOrEmpty(n.NextID)) targets.Add(n.NextID);
                if (n.Choices != null)
                {
                    foreach (var c in n.Choices)
                    {
                        if (!string.IsNullOrEmpty(c.TargetID)) targets.Add(c.TargetID);
                    }
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

            var levels = new Dictionary<string, int>();
            var queue = new Queue<string>();

            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                    levels[kvp.Key] = 0;
                }
            }

            if (queue.Count == 0 && nodes.Count > 0)
            {
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
                        else if (levels[childID] < currentLevel + 1)
                        {
                            levels[childID] = currentLevel + 1;
                            if (levels[childID] < nodes.Count)
                                queue.Enqueue(childID);
                        }
                    }
                }
            }

            foreach (var n in nodes)
            {
                if (!levels.ContainsKey(n.ID)) levels[n.ID] = 0;
            }

            return levels;
        }

        private int CompareNodeIDs(ConversationUnit a, ConversationUnit b)
        {
            string idA = a.ID ?? "";
            string idB = b.ID ?? "";

            var partsA = idA.Split('_');
            var partsB = idB.Split('_');

            if (partsA.Length > 0 && partsB.Length > 0)
            {
                string suffixA = partsA[partsA.Length - 1];
                string suffixB = partsB[partsB.Length - 1];

                if (int.TryParse(suffixA, out int numA) && int.TryParse(suffixB, out int numB))
                {
                    string prefixA = string.Join("_", partsA.Take(partsA.Length - 1));
                    string prefixB = string.Join("_", partsB.Take(partsB.Length - 1));

                    int prefixCompare = string.Compare(prefixA, prefixB);
                    if (prefixCompare != 0) return prefixCompare;
                    return numA.CompareTo(numB);
                }
            }

            return EditorUtility.NaturalCompare(idA, idB);
        }
    }
}
