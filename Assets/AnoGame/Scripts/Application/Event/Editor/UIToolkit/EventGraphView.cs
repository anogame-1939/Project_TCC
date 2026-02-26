#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoFlow.Editor
{
    public class EventGraphView : GraphView
    {
        public Action OnGraphDataChanged;
        public Action<bool> OnExpandAllStateChanged;
        private List<EventData> _eventDataList;
        private HashSet<string> _knownItemIds;
        private Dictionary<string, string> _itemNameMap;
        private Dictionary<string, EventNodeView> _nodeMap = new Dictionary<string, EventNodeView>();
        private bool _editMode = false;
        private bool _showNegativeEdges = false;

        // 遅延保存用
        private IVisualElementScheduledItem _pendingSave;

        /// <summary>
        /// Shared section visibility state for all nodes.
        /// Toggled by toolbar buttons.
        /// </summary>
        public SectionVisibility SectionVis { get; } = new SectionVisibility();

        public EventGraphView()
        {
            // TrackpadPanManipulator must be registered before ContentZoomer
            // so that unmodified scroll events are consumed as pan (trackpad two-finger swipe)
            // while Ctrl/Cmd + scroll is left for ContentZoomer (pinch zoom).
            this.AddManipulator(new TrackpadPanManipulator());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // ノード移動時に座標を自動保存
            graphViewChanged = OnGraphViewChanged;

            // 削除操作をソフトデリートにフック
            deleteSelection = OnDeleteSelection;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                // 遅延保存: ドラッグ終了後500msで保存
                if (_pendingSave != null)
                    _pendingSave.Pause();
                var item = schedule.Execute(SaveNodePositions);
                item.ExecuteLater(500);
                _pendingSave = item;
            }
            return change;
        }
        /// <summary>
        /// GraphView の Delete キー/メニューから呼ばれる削除処理をソフトデリートに差し替え
        /// </summary>
        private void OnDeleteSelection(string operationName, AskUser askUser)
        {
            var nodesToDelete = new List<EventNodeView>();
            foreach (var sel in selection)
            {
                if (sel is EventNodeView nodeView)
                    nodesToDelete.Add(nodeView);
            }

            if (nodesToDelete.Count == 0) return;

            foreach (var nodeView in nodesToDelete)
            {
                DeleteEventNode(nodeView);
            }
        }

        /// <summary>
        /// 空白エリア右クリック時のコンテキストメニュー
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                var mousePos = evt.localMousePosition;
                evt.menu.AppendAction("新規ノード作成", _ => CreateNewEventNode(mousePos));
            }
            else if (evt.target is EventNodeView targetNode)
            {
                evt.menu.AppendAction("ノード削除", _ => DeleteEventNode(targetNode));
            }
            base.BuildContextualMenu(evt);
        }

        /// <summary>
        /// 新規 EventData アセットを作成し、グラフにノードを追加する
        /// </summary>
        public void CreateNewEventNode(Vector2 graphLocalPos)
        {
            if (_eventDataList == null) return;

            // 次の連番を算出
            int maxNum = 0;
            foreach (var ed in _eventDataList)
            {
                var id = ed.EventId;
                if (id != null && id.StartsWith("EV_") && id.Length >= 6)
                {
                    if (int.TryParse(id.Substring(3, 3), out int num))
                    {
                        if (num > maxNum) maxNum = num;
                    }
                }
            }
            int nextNum = maxNum + 1;
            string tempEventId = $"EV_{nextNum:D3}";
            string tempEventName = "";

            // EventData アセットを作成
            var newData = ScriptableObject.CreateInstance<EventData>();
            var so = new SerializedObject(newData);
            so.FindProperty("eventId").stringValue = tempEventId;
            so.FindProperty("eventName").stringValue = tempEventName;
            so.FindProperty("category").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();

            string dir = "Assets/AnoGame/Data/Events/Story2";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{tempEventId}.asset");
            AssetDatabase.CreateAsset(newData, assetPath);
            AssetDatabase.SaveAssets();
            Undo.RegisterCreatedObjectUndo(newData, "Create Event Node");

            // _eventDataList に追加して RebuildGraph
            _eventDataList.Add(newData);
            Debug.Log($"[EventGraph] CreateNewEventNode: {tempEventId} 作成。_eventDataList件数={_eventDataList.Count}");

            // ワールド座標→グラフ座標に変換
            var worldPos = contentViewContainer.WorldToLocal(this.LocalToWorld(graphLocalPos));

            RebuildGraph(null);

            // 作成したノードを所定位置に配置
            if (_nodeMap.TryGetValue(tempEventId, out var nodeView))
            {
                nodeView.SetPosition(new Rect(worldPos.x, worldPos.y, 0, 0));

                // meta.json に即座に保存
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                var meta = EventGraphMeta.Load();
                meta.SetNodePosition(assetGuid, tempEventId, worldPos);
                meta.Save();

                // 名前編集モードで開始
                nodeView.IsNewNode = true;
                nodeView.OnNameEditCancelled = () => DeleteEventNode(nodeView);
                schedule.Execute(() => nodeView.EnterEditNameMode()).ExecuteLater(100);
            }

            OnGraphDataChanged?.Invoke();
        }

        /// <summary>
        /// ノードと対応する EventData アセットを削除する
        /// </summary>
        public void DeleteEventNode(EventNodeView nodeView)
        {
            if (nodeView == null || nodeView.EventData == null) return;

            var data = nodeView.EventData;
            var assetPath = AssetDatabase.GetAssetPath(data);
            Debug.Log($"[EventGraph] DeleteEventNode: {data.EventId}, isDeleted前={data.IsDeleted}, path={assetPath}");

            // ソフトデリート（Undo 対応）
            Undo.RecordObject(data, "Delete Event Node");
            data.IsDeleted = true;
            EditorUtility.SetDirty(data);

            Debug.Log($"[EventGraph] DeleteEventNode: {data.EventId}, isDeleted後={data.IsDeleted}");

            // グラフを再構築（isDeleted のノードが非表示になる）
            RebuildGraph(null);
        }

        public void PopulateGraph(List<EventData> eventDataList, HashSet<string> knownItemIds, Dictionary<string, string> itemNameMap = null)
        {
            _eventDataList = eventDataList;
            _knownItemIds = knownItemIds;
            _itemNameMap = itemNameMap ?? new Dictionary<string, string>();

            // Clear
            DeleteElements(graphElements);
            _nodeMap.Clear();

            if (_eventDataList == null || _eventDataList.Count == 0) return;

            // null エントリとソフトデリート済みをフィルタ
            _eventDataList.RemoveAll(e => e == null);
            var deletedIds = _eventDataList.Where(e => e.IsDeleted).Select(e => e.EventId).ToList();
            if (deletedIds.Count > 0)
                Debug.Log($"[EventGraph] PopulateGraph: isDeletedでスキップ: {string.Join(", ", deletedIds)}");
            var activeList = _eventDataList.Where(e => !e.IsDeleted).ToList();
            Debug.Log($"[EventGraph] PopulateGraph: 全{_eventDataList.Count}件, アクティブ{activeList.Count}件");

            // Build known event ID set
            var knownEventIds = new HashSet<string>();
            foreach (var ed in activeList)
            {
                knownEventIds.Add(ed.EventId);
            }

            // Build tag→eventId lookup: which events produce each resultTag
            var tagProducers = new Dictionary<string, List<string>>();
            foreach (var ed in activeList)
            {
                var tags = ed.ResultTags;
                if (tags == null) continue;
                foreach (var tag in tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    if (!tagProducers.ContainsKey(tag))
                        tagProducers[tag] = new List<string>();
                    tagProducers[tag].Add(ed.EventId);
                }
            }

            // Collect all known tags for validation
            var allKnownTags = new HashSet<string>(tagProducers.Keys);

            // Build eventId→eventName lookup
            var eventNameMap = new Dictionary<string, string>();
            foreach (var ed in activeList)
            {
                eventNameMap[ed.EventId] = ed.EventName;
            }

            // 1. Create Nodes
            foreach (var ed in activeList)
            {
                var node = new EventNodeView(ed, knownEventIds, knownItemIds ?? new HashSet<string>(),
                    activeList, allKnownTags, eventNameMap, _itemNameMap, SectionVis);
                node.OnRequiredEventsChanged = () => ConnectEdgesForNode(node);
                node.OnTagsChanged = () => RebuildGraph(null);
                AddElement(node);
                _nodeMap[ed.EventId] = node;
            }

            // 2. Create Edges from RequiredEventIds (legacy direct references)
            foreach (var ed in activeList)
            {
                if (!_nodeMap.ContainsKey(ed.EventId)) continue;
                var targetNode = _nodeMap[ed.EventId];

                var reqEvents = ed.RequiredEventIds;
                if (reqEvents != null)
                {
                    foreach (var reqId in reqEvents)
                    {
                        if (_nodeMap.TryGetValue(reqId, out var sourceNode))
                        {
                            if (targetNode.ConditionPorts.TryGetValue(reqId, out var condPort))
                            {
                                var edge = sourceNode.OutputPort.ConnectTo(condPort);
                                AddElement(edge);
                            }
                        }
                    }
                }
            }

            // 3. Create Edges from Tags (conditionTags ↔ resultTags)
            foreach (var ed in activeList)
            {
                if (!_nodeMap.ContainsKey(ed.EventId)) continue;
                var targetNode = _nodeMap[ed.EventId];

                var cTags = ed.ConditionTags;
                if (cTags == null) continue;

                foreach (var tag in cTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    // ネガティブタグはここではスキップ（後のブロックで処理）
                    if (tag.StartsWith("!")) continue;
                    if (!tagProducers.TryGetValue(tag, out var producers)) continue;

                    foreach (var producerId in producers)
                    {
                        if (producerId == ed.EventId) continue; // skip self
                        if (!_nodeMap.TryGetValue(producerId, out var sourceNode)) continue;

                        // Connect source OutputPort → target's tag condition port
                        if (targetNode.TagConditionPorts.TryGetValue(tag, out var tagPort))
                        {
                            var edge = sourceNode.OutputPort.ConnectTo(tagPort);
                            AddElement(edge);
                        }
                    }
                }
            }

            // 4. Create Negative Edges from conditionTags with "!" prefix
            if (_showNegativeEdges)
            {
                foreach (var ed in activeList)
                {
                    if (!_nodeMap.ContainsKey(ed.EventId)) continue;
                    var targetNode = _nodeMap[ed.EventId];

                    var cTags2 = ed.ConditionTags;
                    if (cTags2 == null) continue;

                    foreach (var tag in cTags2)
                    {
                        if (string.IsNullOrEmpty(tag) || !tag.StartsWith("!")) continue;
                        var realTag = tag.Substring(1);
                        if (!tagProducers.TryGetValue(realTag, out var producers)) continue;

                        foreach (var producerId in producers)
                        {
                            if (producerId == ed.EventId) continue;
                            if (!_nodeMap.TryGetValue(producerId, out var sourceNode)) continue;

                            if (targetNode.NegativeTagPorts.TryGetValue(tag, out var negPort))
                            {
                                var edge = sourceNode.OutputPort.ConnectTo(negPort);
                                AddElement(edge);
                            }
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
                    // アセットGUIDで座標を検索（フォールバック: eventId）
                    var assetPath = AssetDatabase.GetAssetPath(kvp.Value.EventData);
                    var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    var pos = meta.GetNodePosition(assetGuid);
                    if (!pos.HasValue)
                    {
                        // 旧形式（eventIdキー）からのフォールバック
                        pos = meta.GetNodePositionByEventId(kvp.Key);
                    }
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

            // Re-apply edit mode after rebuild
            SetEditMode(_editMode);

            // エッジレイヤーをノードレイヤーの前面に移動
            schedule.Execute(BringEdgesToFront);
        }

        /// <summary>
        /// GraphView内部のレイヤー構造を操作し、エッジが含まれるレイヤーを
        /// 最前面に移動してノードの上にエッジが描画されるようにする。
        /// </summary>
        private void BringEdgesToFront()
        {
            var layers = contentViewContainer.Children().ToList();
            foreach (var layer in layers)
            {
                bool containsEdge = false;
                foreach (var child in layer.Children())
                {
                    if (child is Edge)
                    {
                        containsEdge = true;
                        break;
                    }
                }
                if (containsEdge)
                {
                    layer.BringToFront();
                }
            }
        }

        /// <summary>
        /// Rebuild the graph preserving existing node positions.
        /// Optionally focus camera on a specific node.
        /// </summary>
        private void RebuildGraph(string focusNodeId, bool preserveSectionStates = true)
        {
            // Save current positions before rebuild
            var positions = new Dictionary<string, Vector2>();
            foreach (var kvp in _nodeMap)
            {
                var rect = kvp.Value.GetPosition();
                positions[kvp.Key] = new Vector2(rect.x, rect.y);
            }

            // Save per-node section states before rebuild (only when preserving)
            Dictionary<string, bool[]> sectionStates = null;
            if (preserveSectionStates)
            {
                sectionStates = new Dictionary<string, bool[]>();
                foreach (var kvp in _nodeMap)
                    sectionStates[kvp.Key] = kvp.Value.GetCurrentSectionStates();
            }

            // Rebuild
            PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);

            // Restore all saved positions
            foreach (var kvp in positions)
            {
                if (_nodeMap.TryGetValue(kvp.Key, out var node))
                    node.SetPosition(new Rect(kvp.Value.x, kvp.Value.y, 0, 0));
            }

            // Restore per-node section states (only when preserving)
            if (sectionStates != null)
            {
                foreach (var kvp in sectionStates)
                {
                    if (_nodeMap.TryGetValue(kvp.Key, out var node))
                        node.ApplySectionStates(kvp.Value);
                }
            }

            // Focus camera on a specific node
            if (!string.IsNullOrEmpty(focusNodeId) && _nodeMap.TryGetValue(focusNodeId, out var focusNode))
            {
                schedule.Execute(() =>
                {
                    var nodeRect = focusNode.GetPosition();
                    var center = new Vector3(nodeRect.x + 150, nodeRect.y + 100, 0);
                    UpdateViewTransform(
                        contentViewContainer.transform.position - center + new Vector3(layout.width / 2, layout.height / 2, 0),
                        contentViewContainer.transform.scale
                    );
                }).ExecuteLater(50);
            }

            OnGraphDataChanged?.Invoke();
        }

        /// <summary>
        /// Toggle a section and rebuild.
        /// </summary>
        public void ToggleSection(string sectionName)
        {
            UnityEngine.Debug.Log($"[PortDbg][USER] Toolbar ToggleSection '{sectionName}'");
            switch (sectionName)
            {
                case "ResultTags": SectionVis.ResultTags = !SectionVis.ResultTags; break;
                case "ConditionTags": SectionVis.ConditionTags = !SectionVis.ConditionTags; break;
                case "RequiredEvents": SectionVis.RequiredEvents = !SectionVis.RequiredEvents; break;
                case "RequiredItems": SectionVis.RequiredItems = !SectionVis.RequiredItems; break;
            }
            RebuildGraph(null, false); // Toolbar toggle: don't preserve per-node states
        }

        /// <summary>
        /// Toggle all sections on or off.
        /// </summary>
        public void ToggleAllSections()
        {
            // 現在全展開なら全折畳、そうでなければ全展開
            bool allExpanded = SectionVis.ResultTags && SectionVis.ConditionTags
                            && SectionVis.RequiredEvents && SectionVis.RequiredItems;
            bool expand = !allExpanded;

            SectionVis.ResultTags = expand;
            SectionVis.ConditionTags = expand;
            SectionVis.RequiredEvents = expand;
            SectionVis.RequiredItems = expand;
            RebuildGraph(null, false); // Toolbar toggle: don't preserve per-node states

            OnExpandAllStateChanged?.Invoke(expand);
        }

        /// <summary>
        /// ノードのヘッダークリックでセクションが折りたたまれた時に呼ばれる。
        /// 全展開状態が崩れたらハイライトを解除する。
        /// </summary>
        public void NotifySectionCollapsed()
        {
            OnExpandAllStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Window 側から SectionVis 変更後に呼ぶリビルド。
        /// per-node section states は復元しない（SectionVis の値をそのまま使う）。
        /// </summary>
        public void RebuildForSectionChange()
        {
            RebuildGraph(null, false);
        }

        /// <summary>
        /// Show or hide all edit buttons (+, ▲, ▼, ×) across all nodes.
        /// </summary>
        public void SetEditMode(bool enabled)
        {
            _editMode = enabled;
            foreach (var node in _nodeMap.Values)
            {
                node.ApplyEditMode(enabled);
            }
        }

        /// <summary>
        /// Toggle negative edge visibility and rebuild.
        /// </summary>
        public void SetShowNegativeEdges(bool show)
        {
            _showNegativeEdges = show;
            RebuildGraph(null);
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
                var assetPath = AssetDatabase.GetAssetPath(kvp.Value.EventData);
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                meta.SetNodePosition(assetGuid, kvp.Key, new Vector2(rect.x, rect.y));
            }
            meta.Save();
        }

        /// <summary>
        /// 指定ノードの RequiredEvent ポートのうち未接続のものにエッジを張る。
        /// AddRequiredEvent 後のローカル更新用。
        /// </summary>
        private void ConnectEdgesForNode(EventNodeView targetNode)
        {
            if (targetNode == null) return;

            foreach (var kvp in targetNode.ConditionPorts)
            {
                var reqId = kvp.Key;
                var condPort = kvp.Value;

                // 既に接続済みならスキップ
                if (condPort.connected) continue;

                // ソースノードを探してエッジ接続
                if (_nodeMap.TryGetValue(reqId, out var sourceNode))
                {
                    var edge = sourceNode.OutputPort.ConnectTo(condPort);
                    AddElement(edge);
                }
            }

            // エッジレイヤーを前面に
            schedule.Execute(BringEdgesToFront);
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

            var inputs = new Dictionary<EventNodeView, List<EventNodeView>>();
            var outputs = new Dictionary<EventNodeView, List<EventNodeView>>();

            foreach (var node in allNodes)
            {
                inputs[node] = new List<EventNodeView>();
                outputs[node] = new List<EventNodeView>();
            }

            foreach (var edge in edges.ToList())
            {
                var sourceNode = edge.output.node as EventNodeView;
                var targetNode = edge.input.node as EventNodeView;
                if (sourceNode != null && targetNode != null)
                {
                    if (!outputs[sourceNode].Contains(targetNode))
                        outputs[sourceNode].Add(targetNode);
                    if (!inputs[targetNode].Contains(sourceNode))
                        inputs[targetNode].Add(sourceNode);
                }
            }

            foreach (var node in allNodes)
            {
                if (inputs[node].Count > 0 || outputs[node].Count > 0)
                    connectedNodes.Add(node);
                else
                    unconnectedNodes.Add(node);
            }

            // --- 1. Layout Unconnected Nodes first (top of canvas) ---
            float unconnectedMaxX = 0f;
            float unconnectedMaxY = 0f;

            if (unconnectedNodes.Count > 0)
            {
                unconnectedNodes.Sort((a, b) => string.Compare(a.EventData.EventId, b.EventData.EventId, StringComparison.Ordinal));

                float gridStartY = 0f;
                float gridStartX = 50f;
                float gridXSpacing = 300f;
                float gridYSpacing = 270f;
                int columns = 5;

                for (int i = 0; i < unconnectedNodes.Count; i++)
                {
                    int row = i / columns;
                    int col = i % columns;
                    float x = gridStartX + col * gridXSpacing;
                    float y = gridStartY + row * gridYSpacing;
                    unconnectedNodes[i].SetPosition(new Rect(x, y, 0, 0));

                    if (x + 250f > unconnectedMaxX) unconnectedMaxX = x + 250f;
                    if (y + 200f > unconnectedMaxY) unconnectedMaxY = y + 200f;
                }
            }

            // --- 2. Layout Connected Nodes (Pyramid: Goal on Right) ---
            // Start below unconnected nodes with padding
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

                float xSpacing = 350f;
                float ySpacing = 270f;
                // Goal X = right side of unconnected area, or default 1000
                float startX = Math.Max(unconnectedMaxX, 1000f);
                // Start Y = below unconnected nodes + padding
                float startY = unconnectedMaxY + 100f;
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

                        // Ensure minimum spacing between nodes in same rank
                        nodesInRank = nodesInRank.OrderBy(n => yPositions[n]).ToList();
                        for (int i = 0; i < nodesInRank.Count - 1; i++)
                        {
                            var n1 = nodesInRank[i];
                            var n2 = nodesInRank[i + 1];
                            if (yPositions[n2] - yPositions[n1] < ySpacing)
                                yPositions[n2] = yPositions[n1] + ySpacing;
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
        }

        /// <summary>
        /// Manipulator that converts unmodified scroll (trackpad two-finger swipe) into
        /// a pan operation on the GraphView. When Ctrl/Cmd is held the event is ignored
        /// so that ContentZoomer can handle it as zoom.
        /// </summary>
        private class TrackpadPanManipulator : Manipulator
        {
            /// <summary>
            /// Scaling factor applied to the scroll delta to control pan speed.
            /// </summary>
            private const float PanSpeed = 3f;

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            }

            private void OnWheel(WheelEvent evt)
            {
                // If any modifier key is held, let other manipulators (ContentZoomer) handle it.
                // On macOS trackpad, pinch-to-zoom sends scroll events with Ctrl held.
                if (evt.ctrlKey || evt.commandKey || evt.altKey || evt.shiftKey)
                    return;

                var graphView = target as GraphView;
                if (graphView == null) return;

                // Convert scroll delta into a pan offset.
                // WheelEvent.delta is (x, y, 0); positive y = scroll down.
                Vector3 currentPos = graphView.contentViewContainer.transform.position;
                Vector3 delta = new Vector3(-evt.delta.x, -evt.delta.y, 0f) * PanSpeed;

                graphView.contentViewContainer.transform.position = currentPos + delta;

                evt.StopPropagation();
                evt.PreventDefault();
            }
        }
    }
}
#endif
