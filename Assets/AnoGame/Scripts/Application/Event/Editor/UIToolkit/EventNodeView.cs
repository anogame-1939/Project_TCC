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
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private VisualElement _topPortContainer;
        private VisualElement _bottomPortContainer;

        // Validation context: sets of known IDs provided by the graph
        private HashSet<string> _knownEventIds;
        private HashSet<string> _knownItemIds;

        public EventNodeView(EventData data, HashSet<string> knownEventIds, HashSet<string> knownItemIds)
        {
            EventData = data;
            _knownEventIds = knownEventIds ?? new HashSet<string>();
            _knownItemIds = knownItemIds ?? new HashSet<string>();

            AddToClassList("event-node");
            title = $"{EventData.EventId}\n{EventData.EventName}";

            // ---- Ports ----

            // Top (Input / Prev)
            _topPortContainer = new VisualElement();
            _topPortContainer.AddToClassList("input-port-container");
            InputPort = InstantiatePort(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            _topPortContainer.Add(InputPort);
            Insert(0, _topPortContainer);

            // Bottom (Output / Next)
            _bottomPortContainer = new VisualElement();
            _bottomPortContainer.AddToClassList("output-port-container");
            OutputPort = InstantiatePort(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(bool));
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
            // Double-click: Select + Focus container in Hierarchy
            if (evt.clickCount == 2 && evt.button == 0)
            {
                EventSceneBinder.SelectContainer(EventData.EventId);
                evt.StopPropagation();
            }
            // Single-click: Ping container
            else if (evt.clickCount == 1 && evt.button == 0 && evt.modifiers == EventModifiers.None)
            {
                // Delay to avoid conflicting with selection drag
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

            // Basic Info (Read-only labels)
            AddInfoRow(container, "ID:", EventData.EventId);
            AddInfoRow(container, "Name:", EventData.EventName);
            AddInfoRow(container, "Category:", EventData.Category);
            AddInfoRow(container, "Desc:", EventData.Description);

            // --- Required Events ---
            AddSectionHeader(container, "Required Events:");
            var reqEvents = EventData.RequiredEventIds;
            if (reqEvents != null && reqEvents.Count > 0)
            {
                var section = new VisualElement();
                section.style.paddingLeft = 4;
                foreach (var eid in reqEvents)
                {
                    bool exists = _knownEventIds.Contains(eid);
                    var lbl = new Label($"{(exists ? "\u2713" : "\u2717")} {eid}");
                    lbl.style.color = exists ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
                    section.Add(lbl);
                }
                container.Add(section);
            }
            else
            {
                AddEmptyLabel(container);
            }

            // --- Required Items ---
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

            // --- Results ---
            AddSectionHeader(container, "Results:");
            var results = EventData.Results;
            if (results != null && results.Count > 0)
            {
                var section = new VisualElement();
                section.style.paddingLeft = 4;
                foreach (var res in results)
                {
                    var lbl = new Label($"  {res}");
                    lbl.style.color = new Color(0.7f, 0.85f, 1f);
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
                // No conditions = always executable
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
                // All satisfied
                style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f);
                style.borderLeftWidth = 3;
            }
            else if (satisfied > 0)
            {
                // Partially satisfied
                style.borderLeftColor = new Color(0.9f, 0.8f, 0.3f);
                style.borderLeftWidth = 3;
            }
            else
            {
                // None satisfied
                style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
                style.borderLeftWidth = 1;
            }
        }
    }
}
#endif
