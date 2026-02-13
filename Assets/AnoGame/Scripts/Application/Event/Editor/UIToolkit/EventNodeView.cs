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
        /// Condition ports keyed by RequiredEventId.
        /// Each required event gets its own Input port for edge connection.
        /// </summary>
        public Dictionary<string, Port> ConditionPorts { get; private set; } = new Dictionary<string, Port>();

        /// <summary>
        /// Callback: fires when this node's RequiredEventIds have been edited.
        /// The graph should rebuild edges in response.
        /// </summary>
        public Action OnRequiredEventsChanged;

        private VisualElement _bottomPortContainer;
        private VisualElement _conditionPortsContainer;

        // Context passed from graph
        private HashSet<string> _knownEventIds;
        private HashSet<string> _knownItemIds;
        private List<EventData> _allEventDataList; // for dropdown

        public EventNodeView(EventData data, HashSet<string> knownEventIds, HashSet<string> knownItemIds, List<EventData> allEventDataList)
        {
            EventData = data;
            _knownEventIds = knownEventIds ?? new HashSet<string>();
            _knownItemIds = knownItemIds ?? new HashSet<string>();
            _allEventDataList = allEventDataList ?? new List<EventData>();

            AddToClassList("event-node");
            title = $"{EventData.EventId}\n{EventData.EventName}";

            // ---- Output Port (Bottom) ----
            _bottomPortContainer = new VisualElement();
            _bottomPortContainer.AddToClassList("output-port-container");
            OutputPort = InstantiatePort(Orientation.Horizontal,
                UnityEditor.Experimental.GraphView.Direction.Output,
                Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "Out";
            _bottomPortContainer.Add(OutputPort);
            Add(_bottomPortContainer);

            // ---- Content ----
            CreateContent();

            // ---- Interactions ----
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

            // Basic Info
            AddInfoRow(container, "ID:", EventData.EventId);
            AddInfoRow(container, "Name:", EventData.EventName);
            AddInfoRow(container, "Category:", EventData.Category);
            AddInfoRow(container, "Desc:", EventData.Description);

            // --- Required Events (with individual condition ports + edit buttons) ---
            var reqEventsHeader = new VisualElement();
            reqEventsHeader.style.flexDirection = FlexDirection.Row;
            reqEventsHeader.style.alignItems = Align.Center;
            reqEventsHeader.style.justifyContent = Justify.SpaceBetween;
            reqEventsHeader.style.marginTop = 8;

            var reqEventsLabel = new Label("Required Events:");
            reqEventsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            reqEventsHeader.Add(reqEventsLabel);

            // "+" button to add a new RequiredEvent
            var addBtn = new Button(() => ShowAddRequiredEventMenu()) { text = "+" };
            addBtn.style.width = 22;
            addBtn.style.height = 20;
            addBtn.style.fontSize = 14;
            addBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            addBtn.style.paddingLeft = 0;
            addBtn.style.paddingRight = 0;
            addBtn.style.paddingTop = 0;
            addBtn.style.paddingBottom = 0;
            addBtn.style.marginLeft = 4;
            addBtn.tooltip = "RequiredEvent追加";
            reqEventsHeader.Add(addBtn);

            container.Add(reqEventsHeader);

            // Condition ports area
            var reqEvents = EventData.RequiredEventIds;
            _conditionPortsContainer = new VisualElement();
            _conditionPortsContainer.AddToClassList("condition-ports-container");

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

            container.Add(_conditionPortsContainer);

            // --- Required Items (label only, no ports) ---
            AddSectionHeader(container, "Required Items:");
            var reqItems = EventData.RequiredItemIds;
            if (reqItems != null && reqItems.Count > 0)
            {
                var section = new VisualElement();
                section.style.paddingLeft = 4;
                foreach (var iid in reqItems)
                {
                    bool exists = _knownItemIds.Contains(iid);
                    var lbl = new Label($"{(exists ? "\u2713" : "\u2717")} {iid}");
                    lbl.style.color = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
                    section.Add(lbl);
                }
                container.Add(section);
            }
            else
            {
                AddEmptyLabel(container);
            }

            // --- Condition Status Border ---
            UpdateConditionBorder();

            extensionContainer.Add(container);
            RefreshExpandedState();
        }

        /// <summary>
        /// Add one condition port row: [Port] [✓/✗ label] [× button]
        /// </summary>
        private void AddConditionPortRow(string eid)
        {
            bool exists = _knownEventIds.Contains(eid);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;

            // Input port for this condition
            var condPort = InstantiatePort(Orientation.Horizontal,
                UnityEditor.Experimental.GraphView.Direction.Input,
                Port.Capacity.Single, typeof(bool));
            condPort.portName = "";
            condPort.style.width = 16;
            condPort.style.minWidth = 16;
            row.Add(condPort);

            ConditionPorts[eid] = condPort;

            // Label with validation indicator
            var lbl = new Label($"{(exists ? "\u2713" : "\u2717")} {eid}");
            lbl.style.color = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
            lbl.style.marginLeft = 4;
            lbl.style.flexGrow = 1;
            row.Add(lbl);

            // "×" button to remove this condition
            var capturedEid = eid; // capture for closure
            var removeBtn = new Button(() => RemoveRequiredEvent(capturedEid)) { text = "\u00d7" };
            removeBtn.style.width = 18;
            removeBtn.style.height = 18;
            removeBtn.style.fontSize = 12;
            removeBtn.style.paddingLeft = 0;
            removeBtn.style.paddingRight = 0;
            removeBtn.style.paddingTop = 0;
            removeBtn.style.paddingBottom = 0;
            removeBtn.style.marginLeft = 2;
            removeBtn.style.color = new Color(1f, 0.5f, 0.5f);
            removeBtn.tooltip = $"Remove {eid}";
            row.Add(removeBtn);

            _conditionPortsContainer.Add(row);
        }

        /// <summary>
        /// Show GenericMenu dropdown listing all EventData (excluding self and already-added).
        /// </summary>
        private void ShowAddRequiredEventMenu()
        {
            var currentIds = EventData.RequiredEventIds ?? new List<string>();
            var menu = new GenericMenu();

            // Group by category
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
            {
                menu.AddDisabledItem(new GUIContent("(追加可能なイベントなし)"));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Add a RequiredEventId to the EventData SO and notify the graph.
        /// </summary>
        private void AddRequiredEvent(string eventId)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty("requiredEventIds");
            if (prop == null) return;

            // Check for duplicates
            for (int i = 0; i < prop.arraySize; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                var eidProp = elem.FindPropertyRelative("eventId");
                if (eidProp != null && eidProp.stringValue == eventId) return; // already exists
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

        /// <summary>
        /// Remove a RequiredEventId from the EventData SO and notify the graph.
        /// </summary>
        private void RemoveRequiredEvent(string eventId)
        {
            var so = new SerializedObject(EventData);
            so.Update();

            var prop = so.FindProperty("requiredEventIds");
            if (prop == null) return;

            for (int i = 0; i < prop.arraySize; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                var eidProp = elem.FindPropertyRelative("eventId");
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

        private void AddSectionHeader(VisualElement parent, string text)
        {
            var lbl = new Label(text);
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.marginTop = 8;
            parent.Add(lbl);
        }

        private void AddEmptyLabel(VisualElement parent)
        {
            var lbl = new Label("(None)");
            lbl.style.color = Color.gray;
            lbl.style.paddingLeft = 4;
            parent.Add(lbl);
        }

        private void UpdateConditionBorder()
        {
            var reqEvents = EventData.RequiredEventIds;
            var reqItems = EventData.RequiredItemIds;

            bool hasConditions = (reqEvents != null && reqEvents.Count > 0) || (reqItems != null && reqItems.Count > 0);
            if (!hasConditions)
            {
                style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f);
                style.borderLeftWidth = 3;
                return;
            }

            int total = 0;
            int satisfied = 0;

            if (reqEvents != null)
            {
                foreach (var eid in reqEvents)
                {
                    total++;
                    if (_knownEventIds.Contains(eid)) satisfied++;
                }
            }
            if (reqItems != null)
            {
                foreach (var iid in reqItems)
                {
                    total++;
                    if (_knownItemIds.Contains(iid)) satisfied++;
                }
            }

            if (total > 0 && satisfied == total)
            {
                style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f);
                style.borderLeftWidth = 3;
            }
            else if (satisfied > 0)
            {
                style.borderLeftColor = new Color(0.9f, 0.8f, 0.3f);
                style.borderLeftWidth = 3;
            }
            else
            {
                style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
                style.borderLeftWidth = 1;
            }
        }
    }
}
#endif
