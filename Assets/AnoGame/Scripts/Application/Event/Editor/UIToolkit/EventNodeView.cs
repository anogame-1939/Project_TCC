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
    /// <summary>
    /// Section visibility flags for accordion control.
    /// Managed by EventGraphView and toggled via toolbar.
    /// </summary>
    public class SectionVisibility
    {
        public bool ResultTags = false;
        public bool ConditionTags = true;
        public bool RequiredEvents = false;
        public bool RequiredItems = false;
    }

    public class EventNodeView : Node
    {
        public EventData EventData { get; private set; }
        public Port OutputPort { get; private set; }

        public Dictionary<string, Port> ConditionPorts { get; private set; } = new Dictionary<string, Port>();
        public Dictionary<string, Port> TagConditionPorts { get; private set; } = new Dictionary<string, Port>();
        public Dictionary<string, Port> NegativeTagPorts { get; private set; } = new Dictionary<string, Port>();

        public Action OnRequiredEventsChanged;
        public Action OnTagsChanged;

        private VisualElement _conditionPortsContainer;
        private VisualElement _tagConditionPortsContainer;

        // Accordion sections
        private VisualElement _resultTagsSection;
        private VisualElement _conditionTagsSection;
        private VisualElement _requiredEventsSection;
        private VisualElement _requiredItemsSection;

        private HashSet<string> _knownEventIds;
        private HashSet<string> _knownItemIds;
        private HashSet<string> _allKnownTags;
        private List<EventData> _allEventDataList;
        private Dictionary<string, string> _eventNameMap;
        private Dictionary<string, string> _itemNameMap;
        private SectionVisibility _sectionVisibility;

        public EventNodeView(EventData data, HashSet<string> knownEventIds, HashSet<string> knownItemIds,
            List<EventData> allEventDataList, HashSet<string> allKnownTags,
            Dictionary<string, string> eventNameMap = null, Dictionary<string, string> itemNameMap = null,
            SectionVisibility sectionVisibility = null)
        {
            EventData = data;
            _knownEventIds = knownEventIds ?? new HashSet<string>();
            _knownItemIds = knownItemIds ?? new HashSet<string>();
            _allEventDataList = allEventDataList ?? new List<EventData>();
            _allKnownTags = allKnownTags ?? new HashSet<string>();
            _eventNameMap = eventNameMap ?? new Dictionary<string, string>();
            _itemNameMap = itemNameMap ?? new Dictionary<string, string>();
            _sectionVisibility = sectionVisibility ?? new SectionVisibility();

            AddToClassList("event-node");
            title = $"{EventData.EventId}\n{EventData.EventName}";

            // Output Port
            var bottomContainer = new VisualElement();
            bottomContainer.AddToClassList("output-port-container");
            OutputPort = InstantiatePort(Orientation.Horizontal,
                UnityEditor.Experimental.GraphView.Direction.Output,
                Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "Out";
            bottomContainer.Add(OutputPort);
            Add(bottomContainer);

            CreateContent();

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenu);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == 0)
            {
                EventSceneBinder.SelectContainer(EventData.EventId);
                evt.StopPropagation();
            }
            else if (evt.clickCount == 1 && evt.button == 0 && evt.modifiers == EventModifiers.None)
            {
                schedule.Execute(() => EventSceneBinder.PingContainer(EventData.EventId)).ExecuteLater(200);
            }
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Ping Container", _ => EventSceneBinder.PingContainer(EventData.EventId));
            evt.menu.AppendAction("Ping Receptor", _ => EventSceneBinder.PingReceptor(EventData.EventId));
            evt.menu.AppendAction("Ping Trigger", _ => EventSceneBinder.PingTrigger(EventData.EventId));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Select in Inspector", _ =>
            {
                Selection.activeObject = EventData;
                EditorGUIUtility.PingObject(EventData);
            });
        }

        private void CreateContent()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingBottom = 4;

            // Compact info: Category only (ID/Name are in title)
            if (!string.IsNullOrEmpty(EventData.Category))
            {
                var catLabel = new Label(EventData.Category);
                catLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                catLabel.style.fontSize = 12;
                catLabel.style.marginBottom = 2;
                container.Add(catLabel);
            }

            // --- Result Tags (accordion) ---
            _resultTagsSection = BuildAccordionSection(container, "Result Tags",
                _sectionVisibility.ResultTags, BuildResultTagsContent,
                () => ShowAddTagMenu("resultTags", OnTagsChanged));

            // --- Condition Tags (accordion, with ports) ---
            _conditionTagsSection = BuildAccordionSection(container, "Condition Tags",
                _sectionVisibility.ConditionTags, BuildConditionTagsContent,
                () => ShowAddTagMenu("conditionTags", OnTagsChanged));

            // --- Required Events (accordion, with ports) ---
            _requiredEventsSection = BuildAccordionSection(container, "Req Events",
                _sectionVisibility.RequiredEvents, BuildRequiredEventsContent,
                () => ShowAddRequiredEventMenu());

            // --- Required Items (accordion, no add button) ---
            _requiredItemsSection = BuildAccordionSection(container, "Req Items",
                _sectionVisibility.RequiredItems, BuildRequiredItemsContent, null);

            UpdateConditionBorder();

            extensionContainer.Add(container);
            RefreshExpandedState();

            // Defer port position update after initial layout
            _portPositionDirty = true;

            // Single GeometryChanged handler on the node itself detects:
            // 1. Initial layout completion → applies port positions
            // 2. Expand/collapse state changes → syncs extension container + repositions ports
            // 3. Section toggle layout changes → repositions ports
            this.RegisterCallback<GeometryChangedEvent>(_ => OnNodeGeometryChanged());
        }

        /// <summary>
        /// Track the last known expanded state to detect changes.
        /// </summary>
        private bool _lastExpanded = true;

        /// <summary>
        /// When true, the next GeometryChanged will trigger port position recalculation.
        /// Using this flag instead of frame-delayed schedules ensures layout is settled.
        /// </summary>
        private bool _portPositionDirty = false;

        /// <summary>
        /// Synchronize extensionContainer style with the current expanded state.
        /// When collapsed, override display:none with position:absolute + height:0
        /// so ports remain in the layout tree for edge routing.
        /// Idempotent: skips if expanded state hasn't changed since last call.
        /// </summary>
        private void SyncExtensionContainerState()
        {
            if (extensionContainer == null) return;

            // Skip if no state change
            if (expanded == _lastExpanded) return;

            if (!expanded)
            {
                extensionContainer.style.display = DisplayStyle.Flex;
                extensionContainer.style.position = Position.Absolute;
                extensionContainer.style.height = 0;
                extensionContainer.style.overflow = Overflow.Hidden;
                extensionContainer.style.left = 0;
                extensionContainer.style.right = 0;
                extensionContainer.style.top = 0;
            }
            else
            {
                extensionContainer.style.position = Position.Relative;
                extensionContainer.style.height = new StyleLength(StyleKeyword.Auto);
                extensionContainer.style.overflow = Overflow.Visible;
                extensionContainer.style.top = StyleKeyword.Null;
            }

            _lastExpanded = expanded;
            SchedulePortPositionUpdate();
        }

        /// <summary>
        /// Schedule port position recalculation.
        /// Resets transforms immediately, then marks dirty so the next
        /// GeometryChanged event (which fires after layout settles)
        /// will apply correct positions.
        /// </summary>
        private void SchedulePortPositionUpdate()
        {
            // Reset all port transforms immediately so layout recalculates from clean state
            ResetPortTransforms();
            // Mark dirty — the ApplyPortPositions will run from the GeometryChanged handler
            _portPositionDirty = true;
        }

        /// <summary>
        /// Reset all port transforms to zero.
        /// </summary>
        private void ResetPortTransforms()
        {
            foreach (var kvp in TagConditionPorts) kvp.Value.transform.position = Vector3.zero;
            foreach (var kvp in NegativeTagPorts) kvp.Value.transform.position = Vector3.zero;
            foreach (var kvp in ConditionPorts) kvp.Value.transform.position = Vector3.zero;
        }

        /// <summary>
        /// Called from GeometryChanged on the node.
        /// If ports are dirty, applies correct positions now that layout is settled.
        /// </summary>
        private void OnNodeGeometryChanged()
        {
            // Check for expand/collapse state change first
            if (expanded != _lastExpanded)
            {
                SyncExtensionContainerState();
                return; // SyncExt will reset transforms and set dirty again
            }

            if (!_portPositionDirty) return;
            _portPositionDirty = false;

            ApplyPortPositions();
        }

        /// <summary>
        /// Apply correct port positions based on the combined state:
        ///   State A: Node collapsed -> all ports to node vertical center
        ///   State B: Node expanded + section collapsed -> ports to section header Y
        ///   State C: Node expanded + section expanded -> ports at normal position (zero)
        /// </summary>
        private void ApplyPortPositions()
        {
            var allPorts = new List<Port>();
            foreach (var kvp in TagConditionPorts) allPorts.Add(kvp.Value);
            foreach (var kvp in NegativeTagPorts) allPorts.Add(kvp.Value);
            foreach (var kvp in ConditionPorts) allPorts.Add(kvp.Value);
            if (allPorts.Count == 0) return;

            bool currentExpanded = expanded;

            // --- State A: Node collapsed ---
            if (!currentExpanded)
            {
                float nodeH = resolvedStyle.height;
                if (nodeH <= 0) return; // layout not ready
                float targetY = worldBound.y + nodeH * 0.5f;

                foreach (var port in allPorts)
                {
                    float portCY = port.worldBound.center.y;
                    port.transform.position = new Vector3(0, targetY - portCY, 0);
                }
                ForceEdgeRepaint();
                return;
            }

            // --- State B/C: Node expanded, check each section ---
            foreach (var port in allPorts)
            {
                var row = port.parent;
                var body = row?.parent;
                if (body == null) continue;

                float bodyHeight = body.resolvedStyle.height;
                if (bodyHeight < 1f)
                {
                    // State B: Section collapsed -> move port to header center Y
                    var section = body.parent;
                    var header = section?.ElementAt(0);
                    if (header != null)
                    {
                        float headerCY = header.worldBound.center.y;
                        float portCY = port.worldBound.center.y;
                        port.transform.position = new Vector3(0, headerCY - portCY, 0);
                    }
                }
                // State C: Section expanded -> transform already zero
            }
            ForceEdgeRepaint();
        }

        /// <summary>
        /// Force edge/connector repaint after port transforms change.
        /// </summary>
        private void ForceEdgeRepaint()
        {
            // Get the GraphView and mark edges for repaint
            var graphView = GetFirstAncestorOfType<GraphView>();
            if (graphView != null)
            {
                foreach (var edge in graphView.edges.ToList())
                {
                    edge.MarkDirtyRepaint();
                    edge.UpdateEdgeControl();
                }
            }
        }

        /// <summary>
        /// Build an accordion section: clickable header + collapsible body.
        /// +ボタンはヘッダー行の右端に配置.
        /// </summary>
        private VisualElement BuildAccordionSection(VisualElement parent, string label,
            bool expanded, Action<VisualElement> buildContent, Action onAddClicked)
        {
            var section = new VisualElement();
            section.style.marginTop = 4;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            // Count for badge
            int count = GetSectionCount(label);
            string arrowDown = "\u25BE"; // small down triangle
            string arrowRight = "\u25B8"; // small right triangle
            string arrow = expanded ? arrowDown : arrowRight;
            var headerLabel = new Label($"{arrow} {label} ({count})");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 12;
            headerLabel.style.color = count > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            headerLabel.style.flexGrow = 1;
            header.Add(headerLabel);

            // + button on header row
            if (onAddClicked != null)
            {
                var addBtn = new Button(() => onAddClicked()) { text = "+" };
                StyleSmallButton(addBtn, $"{label}追加");
                header.Add(addBtn);
            }

            section.Add(header);

            // Body — use height:0 + overflow:hidden instead of display:none
            // to keep ports in the layout tree for edge routing.
            var body = new VisualElement();
            body.style.paddingLeft = 4;
            SetBodyCollapsed(body, !expanded);
            buildContent(body);
            section.Add(body);

            // Click header to toggle body
            var capturedLabel = headerLabel;
            var capturedBody = body;
            var capturedArrowLabel = label;
            var capturedCount = count;
            header.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                bool isVisible = capturedBody.resolvedStyle.height > 0;
                Debug.Log($"[PortDbg][USER] SectionToggle '{capturedArrowLabel}' in '{EventData.EventId}': wasVisible={isVisible} -> collapsed={isVisible} nodeExpanded={expanded}");
                SetBodyCollapsed(capturedBody, isVisible);
                string newArrow = isVisible ? arrowRight : arrowDown;
                capturedLabel.text = $"{newArrow} {capturedArrowLabel} ({capturedCount})";
                SchedulePortPositionUpdate();
                evt.StopPropagation();
            });

            parent.Add(section);
            return section;
        }

        private static void SetBodyCollapsed(VisualElement body, bool collapsed)
        {
            if (collapsed)
            {
                body.style.height = 0;
                body.style.overflow = Overflow.Hidden;
            }
            else
            {
                body.style.height = new StyleLength(StyleKeyword.Auto);
                body.style.overflow = Overflow.Visible;
            }
        }

        private int GetSectionCount(string label)
        {
            switch (label)
            {
                case "Result Tags": return EventData.ResultTags?.Count ?? 0;
                case "Condition Tags": return EventData.ConditionTags?.Count ?? 0;
                case "Req Events": return EventData.RequiredEventIds?.Count ?? 0;
                case "Req Items": return EventData.RequiredItemIds?.Count ?? 0;
                default: return 0;
            }
        }

        // ====== Result Tags Content ======
        private void BuildResultTagsContent(VisualElement body)
        {
            var tags = EventData.ResultTags;
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;

                    var tagLbl = new Label($"\u25cf {tag}");
                    tagLbl.style.color = new Color(0.5f, 0.85f, 1f);
                    tagLbl.style.flexGrow = 1;
                    row.Add(tagLbl);

                    var capturedTag = tag;
                    var removeBtn = new Button(() => RemoveTag("resultTags", capturedTag, OnTagsChanged)) { text = "\u00d7" };
                    StyleRemoveButton(removeBtn, $"Remove {tag}");
                    row.Add(removeBtn);

                    body.Add(row);
                }
            }
        }

        // ====== Condition Tags Content ======
        private void BuildConditionTagsContent(VisualElement body)
        {
            _tagConditionPortsContainer = body;

            var cTags = EventData.ConditionTags;
            if (cTags != null && cTags.Count > 0)
            {
                foreach (var tag in cTags)
                {
                    AddTagConditionPortRow(tag);
                }
            }
        }

        private void AddTagConditionPortRow(string tag)
        {
            bool isNegative = tag.StartsWith("!");
            string realTag = isNegative ? tag.Substring(1) : tag;
            bool satisfied = _allKnownTags.Contains(realTag);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var condPort = InstantiatePort(Orientation.Horizontal,
                UnityEditor.Experimental.GraphView.Direction.Input,
                Port.Capacity.Multi, typeof(bool));
            condPort.portName = "";
            condPort.style.width = 16;
            condPort.style.minWidth = 16;
            row.Add(condPort);

            if (isNegative)
            {
                condPort.portColor = new Color(1f, 0.27f, 0.27f);
                NegativeTagPorts[tag] = condPort;
            }
            else
            {
                TagConditionPorts[tag] = condPort;
            }

            string icon = isNegative ? "\u2717" : (satisfied ? "\u2713" : "\u2717");
            var lbl = new Label($"{icon} {tag}");
            if (isNegative)
            {
                lbl.style.color = new Color(1f, 0.4f, 0.4f);
            }
            else
            {
                lbl.style.color = satisfied ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.5f, 0.5f);
            }
            lbl.style.marginLeft = 4;
            lbl.style.flexGrow = 1;
            row.Add(lbl);

            // Reorder buttons
            var capturedTag = tag;
            var upBtn = new Button(() => MoveTag("conditionTags", capturedTag, -1, OnTagsChanged)) { text = "\u25B2" };
            StyleMiniButton(upBtn, "上へ");
            row.Add(upBtn);

            var downBtn = new Button(() => MoveTag("conditionTags", capturedTag, 1, OnTagsChanged)) { text = "\u25BC" };
            StyleMiniButton(downBtn, "下へ");
            row.Add(downBtn);

            var removeBtn = new Button(() => RemoveTag("conditionTags", capturedTag, OnTagsChanged)) { text = "\u00d7" };
            StyleRemoveButton(removeBtn, $"Remove {tag}");
            row.Add(removeBtn);

            _tagConditionPortsContainer.Add(row);
        }

        // ====== Required Events Content ======
        private void BuildRequiredEventsContent(VisualElement body)
        {
            _conditionPortsContainer = body;

            var reqEvents = EventData.RequiredEventIds;
            if (reqEvents != null && reqEvents.Count > 0)
            {
                foreach (var eid in reqEvents)
                {
                    AddConditionPortRow(eid);
                }
            }
        }

        private void AddConditionPortRow(string eid)
        {
            bool exists = _knownEventIds.Contains(eid);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var condPort = InstantiatePort(Orientation.Horizontal,
                UnityEditor.Experimental.GraphView.Direction.Input,
                Port.Capacity.Single, typeof(bool));
            condPort.portName = "";
            condPort.style.width = 16;
            condPort.style.minWidth = 16;
            row.Add(condPort);

            ConditionPorts[eid] = condPort;

            var displayName = _eventNameMap.TryGetValue(eid, out var eName) ? eName : eid;
            var lbl = new Label($"{(exists ? "\u2713" : "\u2717")} {displayName}");
            lbl.tooltip = eid;
            lbl.style.color = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
            lbl.style.marginLeft = 4;
            lbl.style.flexGrow = 1;
            row.Add(lbl);

            // Reorder buttons
            var capturedEid = eid;
            var upBtn = new Button(() => MoveRequiredEvent(capturedEid, -1)) { text = "\u25B2" };
            StyleMiniButton(upBtn, "上へ");
            row.Add(upBtn);

            var downBtn = new Button(() => MoveRequiredEvent(capturedEid, 1)) { text = "\u25BC" };
            StyleMiniButton(downBtn, "下へ");
            row.Add(downBtn);

            var removeBtn = new Button(() => RemoveRequiredEvent(capturedEid)) { text = "\u00d7" };
            StyleRemoveButton(removeBtn, $"Remove {eid}");
            row.Add(removeBtn);

            _conditionPortsContainer.Add(row);
        }

        // ====== Required Items Content ======
        private void BuildRequiredItemsContent(VisualElement body)
        {
            var reqItems = EventData.RequiredItemIds;
            if (reqItems != null && reqItems.Count > 0)
            {
                foreach (var iid in reqItems)
                {
                    bool exists = _knownItemIds.Contains(iid);
                    var itemDisplayName = _itemNameMap.TryGetValue(iid, out var iName) ? iName : iid;
                    var lbl = new Label($"{(exists ? "\u2713" : "\u2717")} {itemDisplayName}");
                    lbl.tooltip = iid;
                    lbl.style.color = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
                    body.Add(lbl);
                }
            }
        }

        // ====== Tag Add/Remove ======
        private void ShowAddTagMenu(string fieldName, Action callback)
        {
            var menu = new GenericMenu();

            var existingTags = new HashSet<string>();
            foreach (var ed in _allEventDataList)
            {
                foreach (var t in ed.ResultTags) if (!string.IsNullOrEmpty(t)) existingTags.Add(t);
                foreach (var t in ed.ConditionTags) if (!string.IsNullOrEmpty(t)) existingTags.Add(t);
            }

            var currentTags = new HashSet<string>();
            if (fieldName == "resultTags")
                foreach (var t in EventData.ResultTags) currentTags.Add(t);
            else
                foreach (var t in EventData.ConditionTags) currentTags.Add(t);

            var available = existingTags.Where(t => !currentTags.Contains(t) && !t.StartsWith("!")).OrderBy(t => t).ToList();
            foreach (var tag in available)
            {
                var capturedTag = tag;
                menu.AddItem(new GUIContent($"既存/{capturedTag}"), false, () => AddTag(fieldName, capturedTag, callback));
            }

            // conditionTags の場合のみ: resultTags から ! 付きネガティブ候補を生成
            if (fieldName == "conditionTags")
            {
                var resultTags = new HashSet<string>();
                foreach (var ed in _allEventDataList)
                {
                    foreach (var t in ed.ResultTags)
                        if (!string.IsNullOrEmpty(t)) resultTags.Add(t);
                }

                var negCandidates = resultTags
                    .Select(t => "!" + t)
                    .Where(nt => !currentTags.Contains(nt))
                    .OrderBy(t => t)
                    .ToList();

                if (negCandidates.Count > 0)
                {
                    foreach (var neg in negCandidates)
                    {
                        var capturedNeg = neg;
                        menu.AddItem(new GUIContent($"否定/{capturedNeg}"), false, () => AddTag(fieldName, capturedNeg, callback));
                    }
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("新規タグ..."), false, () =>
            {
                var input = EditorInputDialog.Show("New Tag", "タグ名を入力:", "");
                if (!string.IsNullOrEmpty(input))
                {
                    AddTag(fieldName, input.Trim(), callback);
                }
            });

            menu.ShowAsContext();
        }

        private void AddTag(string fieldName, string tag, Action callback)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).stringValue == tag) return;
            }

            int idx = prop.arraySize;
            prop.InsertArrayElementAtIndex(idx);
            prop.GetArrayElementAtIndex(idx).stringValue = tag;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(EventData);
            AssetDatabase.SaveAssetIfDirty(EventData);
            callback?.Invoke();
        }

        private void RemoveTag(string fieldName, string tag, Action callback)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    prop.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(EventData);
                    AssetDatabase.SaveAssetIfDirty(EventData);
                    callback?.Invoke();
                    return;
                }
            }
        }

        // ====== RequiredEvent Add/Remove ======
        private void ShowAddRequiredEventMenu()
        {
            var currentIds = EventData.RequiredEventIds ?? new List<string>();
            var menu = new GenericMenu();

            var grouped = _allEventDataList
                .Where(ed => ed.EventId != EventData.EventId && !currentIds.Contains(ed.EventId))
                .GroupBy(ed => string.IsNullOrEmpty(ed.Category) ? "(未分類)" : ed.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                foreach (var ed in group.OrderBy(e => e.EventId))
                {
                    var capturedId = ed.EventId;
                    menu.AddItem(
                        new GUIContent($"{group.Key}/{ed.EventId} - {ed.EventName}"),
                        false,
                        () => AddRequiredEvent(capturedId)
                    );
                }
            }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("(追加可能なイベントなし)"));

            menu.ShowAsContext();
        }

        private void AddRequiredEvent(string eventId)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty("requiredEventIds");
            if (prop == null) return;

            for (int i = 0; i < prop.arraySize; i++)
            {
                var eidProp = prop.GetArrayElementAtIndex(i).FindPropertyRelative("eventId");
                if (eidProp != null && eidProp.stringValue == eventId) return;
            }

            int idx = prop.arraySize;
            prop.InsertArrayElementAtIndex(idx);
            var newElem = prop.GetArrayElementAtIndex(idx);
            var newEidProp = newElem.FindPropertyRelative("eventId");
            if (newEidProp != null) newEidProp.stringValue = eventId;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(EventData);
            AssetDatabase.SaveAssetIfDirty(EventData);
            OnRequiredEventsChanged?.Invoke();
        }

        private void RemoveRequiredEvent(string eventId)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty("requiredEventIds");
            if (prop == null) return;

            for (int i = 0; i < prop.arraySize; i++)
            {
                var eidProp = prop.GetArrayElementAtIndex(i).FindPropertyRelative("eventId");
                if (eidProp != null && eidProp.stringValue == eventId)
                {
                    prop.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(EventData);
                    AssetDatabase.SaveAssetIfDirty(EventData);
                    OnRequiredEventsChanged?.Invoke();
                    return;
                }
            }
        }

        // ====== Helpers ======
        private void StyleSmallButton(Button btn, string tooltip)
        {
            btn.AddToClassList("edit-btn");
            btn.style.width = 20;
            btn.style.height = 16;
            btn.style.fontSize = 12;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.tooltip = tooltip;
        }

        private void StyleMiniButton(Button btn, string tooltip)
        {
            btn.AddToClassList("edit-btn");
            btn.style.width = 16;
            btn.style.height = 14;
            btn.style.fontSize = 8;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.style.marginLeft = 1;
            btn.style.marginRight = 0;
            btn.tooltip = tooltip;
        }

        private void StyleRemoveButton(Button btn, string tooltip)
        {
            btn.AddToClassList("edit-btn");
            btn.style.width = 16;
            btn.style.height = 16;
            btn.style.fontSize = 10;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.style.marginLeft = 2;
            btn.style.color = new Color(1f, 0.5f, 0.5f);
            btn.tooltip = tooltip;
        }

        /// <summary>
        /// Move a tag up or down in the array. dir: -1 = up, +1 = down.
        /// </summary>
        private void MoveTag(string fieldName, string tag, int dir, Action callback)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            int idx = -1;
            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).stringValue == tag) { idx = i; break; }
            }
            if (idx < 0) return;

            int newIdx = idx + dir;
            if (newIdx < 0 || newIdx >= prop.arraySize) return;

            prop.MoveArrayElement(idx, newIdx);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(EventData);
            AssetDatabase.SaveAssetIfDirty(EventData);
            callback?.Invoke();
        }

        /// <summary>
        /// Move a required event up or down. dir: -1 = up, +1 = down.
        /// </summary>
        private void MoveRequiredEvent(string eventId, int dir)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty("requiredEventIds");
            if (prop == null) return;

            int idx = -1;
            for (int i = 0; i < prop.arraySize; i++)
            {
                var eidProp = prop.GetArrayElementAtIndex(i).FindPropertyRelative("eventId");
                if (eidProp != null && eidProp.stringValue == eventId) { idx = i; break; }
            }
            if (idx < 0) return;

            int newIdx = idx + dir;
            if (newIdx < 0 || newIdx >= prop.arraySize) return;

            prop.MoveArrayElement(idx, newIdx);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(EventData);
            AssetDatabase.SaveAssetIfDirty(EventData);
            OnRequiredEventsChanged?.Invoke();
        }

        private void UpdateConditionBorder()
        {
            var reqEvents = EventData.RequiredEventIds;
            var reqItems = EventData.RequiredItemIds;
            var cTags = EventData.ConditionTags;

            bool hasConditions = (reqEvents != null && reqEvents.Count > 0)
                || (reqItems != null && reqItems.Count > 0)
                || (cTags != null && cTags.Count > 0);

            if (!hasConditions)
            {
                style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f);
                style.borderLeftWidth = 3;
                return;
            }

            int total = 0, satisfied = 0;

            if (reqEvents != null) foreach (var eid in reqEvents) { total++; if (_knownEventIds.Contains(eid)) satisfied++; }
            if (reqItems != null) foreach (var iid in reqItems) { total++; if (_knownItemIds.Contains(iid)) satisfied++; }
            if (cTags != null) foreach (var t in cTags)
                {
                    // ネガティブタグは ! を除いた実タグ名で判定
                    string realTag = t.StartsWith("!") ? t.Substring(1) : t;
                    total++;
                    if (_allKnownTags.Contains(realTag)) satisfied++;
                }

            if (total > 0 && satisfied == total) { style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f); style.borderLeftWidth = 3; }
            else if (satisfied > 0) { style.borderLeftColor = new Color(0.9f, 0.8f, 0.3f); style.borderLeftWidth = 3; }
            else { style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f); style.borderLeftWidth = 1; }
        }
    }

    public class EditorInputDialog : EditorWindow
    {
        private string _input = "";
        private string _message = "";
        private string _result = null;
        private bool _confirmed = false;

        public static string Show(string title, string message, string defaultValue)
        {
            var dialog = CreateInstance<EditorInputDialog>();
            dialog.titleContent = new GUIContent(title);
            dialog._message = message;
            dialog._input = defaultValue ?? "";
            dialog.minSize = new Vector2(300, 100);
            dialog.maxSize = new Vector2(400, 120);
            dialog.ShowModalUtility();
            return dialog._confirmed ? dialog._result : null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_message);
            _input = EditorGUILayout.TextField(_input);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                _result = _input;
                _confirmed = true;
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
