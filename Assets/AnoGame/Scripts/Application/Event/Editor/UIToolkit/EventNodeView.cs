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
    public class EventNodeView : Node
    {
        public EventData EventData { get; private set; }
        public Port OutputPort { get; private set; }

        /// <summary>
        /// Condition ports keyed by RequiredEventId (legacy direct references).
        /// </summary>
        public Dictionary<string, Port> ConditionPorts { get; private set; } = new Dictionary<string, Port>();

        /// <summary>
        /// Condition ports keyed by conditionTag.
        /// </summary>
        public Dictionary<string, Port> TagConditionPorts { get; private set; } = new Dictionary<string, Port>();

        public Action OnRequiredEventsChanged;
        public Action OnTagsChanged;

        private VisualElement _conditionPortsContainer;
        private VisualElement _tagConditionPortsContainer;

        private HashSet<string> _knownEventIds;
        private HashSet<string> _knownItemIds;
        private HashSet<string> _allKnownTags;
        private List<EventData> _allEventDataList;
        private Dictionary<string, string> _eventNameMap;
        private Dictionary<string, string> _itemNameMap;

        public EventNodeView(EventData data, HashSet<string> knownEventIds, HashSet<string> knownItemIds,
            List<EventData> allEventDataList, HashSet<string> allKnownTags,
            Dictionary<string, string> eventNameMap = null, Dictionary<string, string> itemNameMap = null)
        {
            EventData = data;
            _knownEventIds = knownEventIds ?? new HashSet<string>();
            _knownItemIds = knownItemIds ?? new HashSet<string>();
            _allEventDataList = allEventDataList ?? new List<EventData>();
            _allKnownTags = allKnownTags ?? new HashSet<string>();
            _eventNameMap = eventNameMap ?? new Dictionary<string, string>();
            _itemNameMap = itemNameMap ?? new Dictionary<string, string>();

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
            container.style.paddingBottom = 8;

            // --- Basic Info ---
            AddInfoRow(container, "ID:", EventData.EventId);
            AddInfoRow(container, "Name:", EventData.EventName);
            AddInfoRow(container, "Category:", EventData.Category);

            // --- Result Tags ---
            BuildResultTagsSection(container);

            // --- Condition Tags (with ports) ---
            BuildConditionTagsSection(container);

            // --- Required Events (legacy, with ports) ---
            BuildRequiredEventsSection(container);

            // --- Required Items (labels only) ---
            BuildRequiredItemsSection(container);

            // Border
            UpdateConditionBorder();

            extensionContainer.Add(container);
            RefreshExpandedState();
        }

        // ====== Result Tags ======
        private void BuildResultTagsSection(VisualElement parent)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginTop = 8;

            var lbl = new Label("Result Tags:");
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(lbl);

            var addBtn = new Button(() => ShowAddTagMenu("resultTags", OnTagsChanged)) { text = "+" };
            StyleSmallButton(addBtn, "ResultTag追加");
            header.Add(addBtn);

            parent.Add(header);

            var tags = EventData.ResultTags;
            if (tags != null && tags.Count > 0)
            {
                var section = new VisualElement();
                section.style.paddingLeft = 4;
                foreach (var tag in tags)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.marginTop = 1;

                    var tagLbl = new Label($"\u25cf {tag}");
                    tagLbl.style.color = new Color(0.5f, 0.85f, 1f);
                    tagLbl.style.flexGrow = 1;
                    row.Add(tagLbl);

                    var capturedTag = tag;
                    var removeBtn = new Button(() => RemoveTag("resultTags", capturedTag, OnTagsChanged)) { text = "\u00d7" };
                    StyleRemoveButton(removeBtn, $"Remove {tag}");
                    row.Add(removeBtn);

                    section.Add(row);
                }
                parent.Add(section);
            }
            else
            {
                AddEmptyLabel(parent);
            }
        }

        // ====== Condition Tags ======
        private void BuildConditionTagsSection(VisualElement parent)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginTop = 8;

            var lbl = new Label("Condition Tags:");
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(lbl);

            var addBtn = new Button(() => ShowAddTagMenu("conditionTags", OnTagsChanged)) { text = "+" };
            StyleSmallButton(addBtn, "ConditionTag追加");
            header.Add(addBtn);

            parent.Add(header);

            _tagConditionPortsContainer = new VisualElement();
            _tagConditionPortsContainer.AddToClassList("condition-ports-container");

            var cTags = EventData.ConditionTags;
            if (cTags != null && cTags.Count > 0)
            {
                foreach (var tag in cTags)
                {
                    AddTagConditionPortRow(tag);
                }
            }
            else
            {
                var emptyLbl = new Label("(None)");
                emptyLbl.style.color = Color.gray;
                emptyLbl.style.paddingLeft = 4;
                _tagConditionPortsContainer.Add(emptyLbl);
            }
            parent.Add(_tagConditionPortsContainer);
        }

        private void AddTagConditionPortRow(string tag)
        {
            bool satisfied = _allKnownTags.Contains(tag);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;

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

            var capturedTag = tag;
            var removeBtn = new Button(() => RemoveTag("conditionTags", capturedTag, OnTagsChanged)) { text = "\u00d7" };
            StyleRemoveButton(removeBtn, $"Remove {tag}");
            row.Add(removeBtn);

            _tagConditionPortsContainer.Add(row);
        }

        // ====== Required Events (Legacy) ======
        private void BuildRequiredEventsSection(VisualElement parent)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginTop = 8;

            var lbl = new Label("Required Events:");
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color = new Color(0.7f, 0.7f, 0.7f);
            header.Add(lbl);

            var addBtn = new Button(() => ShowAddRequiredEventMenu()) { text = "+" };
            StyleSmallButton(addBtn, "RequiredEvent追加");
            header.Add(addBtn);

            parent.Add(header);

            _conditionPortsContainer = new VisualElement();
            _conditionPortsContainer.AddToClassList("condition-ports-container");

            var reqEvents = EventData.RequiredEventIds;
            if (reqEvents != null && reqEvents.Count > 0)
            {
                foreach (var eid in reqEvents)
                {
                    AddConditionPortRow(eid);
                }
            }
            else
            {
                var emptyLbl = new Label("(None)");
                emptyLbl.style.color = Color.gray;
                emptyLbl.style.paddingLeft = 4;
                _conditionPortsContainer.Add(emptyLbl);
            }
            parent.Add(_conditionPortsContainer);
        }

        private void AddConditionPortRow(string eid)
        {
            bool exists = _knownEventIds.Contains(eid);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;

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

            var capturedEid = eid;
            var removeBtn = new Button(() => RemoveRequiredEvent(capturedEid)) { text = "\u00d7" };
            StyleRemoveButton(removeBtn, $"Remove {eid}");
            row.Add(removeBtn);

            _conditionPortsContainer.Add(row);
        }

        // ====== Required Items ======
        private void BuildRequiredItemsSection(VisualElement parent)
        {
            var header = new Label("Required Items:");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 8;
            header.style.color = new Color(0.7f, 0.7f, 0.7f);
            parent.Add(header);

            var reqItems = EventData.RequiredItemIds;
            if (reqItems != null && reqItems.Count > 0)
            {
                var section = new VisualElement();
                section.style.paddingLeft = 4;
                foreach (var iid in reqItems)
                {
                    bool exists = _knownItemIds.Contains(iid);
                    var itemDisplayName = _itemNameMap.TryGetValue(iid, out var iName) ? iName : iid;
                    var lbl = new Label($"{(exists ? "\u2713" : "\u2717")} {itemDisplayName}");
                    lbl.tooltip = iid;
                    lbl.style.color = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
                    section.Add(lbl);
                }
                parent.Add(section);
            }
            else
            {
                AddEmptyLabel(parent);
            }
        }

        // ====== Tag Add/Remove ======
        private void ShowAddTagMenu(string fieldName, Action callback)
        {
            var menu = new GenericMenu();

            // Gather existing tags from all events for suggestions
            var existingTags = new HashSet<string>();
            foreach (var ed in _allEventDataList)
            {
                foreach (var t in ed.ResultTags) if (!string.IsNullOrEmpty(t)) existingTags.Add(t);
                foreach (var t in ed.ConditionTags) if (!string.IsNullOrEmpty(t)) existingTags.Add(t);
            }

            // Get current tags on this event for this field to exclude
            var currentTags = new HashSet<string>();
            if (fieldName == "resultTags")
                foreach (var t in EventData.ResultTags) currentTags.Add(t);
            else
                foreach (var t in EventData.ConditionTags) currentTags.Add(t);

            // Existing tags section
            var available = existingTags.Where(t => !currentTags.Contains(t)).OrderBy(t => t).ToList();
            foreach (var tag in available)
            {
                var capturedTag = tag;
                menu.AddItem(new GUIContent($"Existing/{capturedTag}"), false, () => AddTag(fieldName, capturedTag, callback));
            }

            // New tag entry (opens input dialog)
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("New Tag..."), false, () =>
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

            // Duplicate check
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

            Debug.Log($"[EventGraph] Added tag '{tag}' to {EventData.EventId}.{fieldName}");
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

                    Debug.Log($"[EventGraph] Removed tag '{tag}' from {EventData.EventId}.{fieldName}");
                    callback?.Invoke();
                    return;
                }
            }
        }

        // ====== RequiredEvent Add/Remove (Legacy) ======
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

            Debug.Log($"[EventGraph] Added RequiredEvent '{eventId}' to {EventData.EventId}");
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

                    Debug.Log($"[EventGraph] Removed RequiredEvent '{eventId}' from {EventData.EventId}");
                    OnRequiredEventsChanged?.Invoke();
                    return;
                }
            }
        }

        // ====== Helpers ======
        private void AddInfoRow(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 2;
            var lbl = new Label(label);
            lbl.style.minWidth = 70;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(lbl);
            var val = new Label(string.IsNullOrEmpty(value) ? "-" : value);
            val.style.flexShrink = 1;
            row.Add(val);
            parent.Add(row);
        }

        private void AddEmptyLabel(VisualElement parent)
        {
            var lbl = new Label("(None)");
            lbl.style.color = Color.gray;
            lbl.style.paddingLeft = 4;
            parent.Add(lbl);
        }

        private void StyleSmallButton(Button btn, string tooltip)
        {
            btn.style.width = 22;
            btn.style.height = 20;
            btn.style.fontSize = 14;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.style.marginLeft = 4;
            btn.tooltip = tooltip;
        }

        private void StyleRemoveButton(Button btn, string tooltip)
        {
            btn.style.width = 18;
            btn.style.height = 18;
            btn.style.fontSize = 12;
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.style.marginLeft = 2;
            btn.style.color = new Color(1f, 0.5f, 0.5f);
            btn.tooltip = tooltip;
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

    /// <summary>
    /// Simple editor input dialog for entering new tag names.
    /// </summary>
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
