using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventNodeView : Node
    {
        public EventJsonItem Data { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private TextField _idField;
        private TextField _nameField;
        private TextField _categoryField;
        private TextField _descriptionField;

        // Custom containers for ports
        private VisualElement _topPortContainer;
        private VisualElement _bottomPortContainer;

        private Action _onDataChanged;

        public EventNodeView(EventJsonItem data, Action onDataChanged)
        {
            Data = data;
            _onDataChanged = onDataChanged;

            AddToClassList("event-node");
            title = $"{Data.eventId}\n{Data.name}";

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

            // Default position (if we had position data, we'd use it)
            // But JSON doesn't store position.
            // So we rely on auto layout or GraphView persistence?
            // GraphView persistence is tricky without saving position to file.
            // For now, let's just use auto layout every time or rely on runtime layout.
            // Or maybe add position fields to JSON? No, I shouldn't modify the data format if I can avoid it.
            // The original IMGUI version runs AutoLayout every time or on button press.
        }

        private void CreateContent()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingBottom = 8;

            // ID Field
            _idField = new TextField("ID: ");
            _idField.value = Data.eventId;
            _idField.RegisterValueChangedCallback(evt =>
            {
                Data.eventId = evt.newValue;
                title = $"{Data.eventId}\n{Data.name}";
                _onDataChanged?.Invoke();
            });
            container.Add(_idField);

            // Name Field
            _nameField = new TextField("Name: ");
            _nameField.value = Data.name;
            _nameField.RegisterValueChangedCallback(evt =>
            {
                Data.name = evt.newValue;
                title = $"{Data.eventId}\n{Data.name}";
                _onDataChanged?.Invoke();
            });
            container.Add(_nameField);

            // Category Field
            _categoryField = new TextField("Category: ");
            _categoryField.value = Data.category;
            _categoryField.RegisterValueChangedCallback(evt =>
            {
                Data.category = evt.newValue;
                _onDataChanged?.Invoke();
            });
            container.Add(_categoryField);

            // Description Field
            _descriptionField = new TextField("Desc: ");
            _descriptionField.multiline = true;
            _descriptionField.value = Data.description;
            _descriptionField.AddToClassList("node-text-area");
            _descriptionField.RegisterValueChangedCallback(evt =>
            {
                Data.description = evt.newValue;
                _onDataChanged?.Invoke();
            });
            container.Add(_descriptionField);

            extensionContainer.Add(container);
            RefreshExpandedState();
        }
    }
}
