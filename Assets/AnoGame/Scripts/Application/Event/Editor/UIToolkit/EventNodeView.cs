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
            string arrow = expanded ? "\u25BC" : "\u25B6";
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

            // Body
            var body = new VisualElement();
            body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            body.style.paddingLeft = 4;
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
                bool isVisible = capturedBody.style.display == DisplayStyle.Flex;
                capturedBody.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
                string newArrow = isVisible ? "\u25B6" : "\u25BC";
                capturedLabel.text = $"{newArrow} {capturedArrowLabel} ({capturedCount})";
                evt.StopPropagation();
            });

            parent.Add(section);
            return section;
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
            bool satisfied = _allKnownTags.Contains(tag);

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

            TagConditionPorts[tag] = condPort;

            var lbl = new Label($"{(satisfied ? "\u2713" : "\u2717")} {tag}");
            lbl.style.color = satisfied ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.5f, 0.5f);
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

            var available = existingTags.Where(t => !currentTags.Contains(t)).OrderBy(t => t).ToList();
            foreach (var tag in available)
            {
                var capturedTag = tag;
                menu.AddItem(new GUIContent($"既存/{capturedTag}"), false, () => AddTag(fieldName, capturedTag, callback));
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
            if (cTags != null) foreach (var t in cTags) { total++; if (_allKnownTags.Contains(t)) satisfied++; }

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
