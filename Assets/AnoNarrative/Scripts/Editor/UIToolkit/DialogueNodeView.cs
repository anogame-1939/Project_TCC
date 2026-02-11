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
    /// GraphView Node representing a single ConversationUnit.
    /// Ports are placed top (In) and bottom (Next) for natural vertical flow.
    /// </summary>
    public class DialogueNodeView : Node
    {
        public ConversationUnit Unit { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        /// <summary>Choice output ports.</summary>
        private readonly List<Port> _choicePorts = new List<Port>();
        public IReadOnlyList<Port> ChoicePorts => _choicePorts;

        private PopupField<string> _speakerDropdown;
        private TextField _bodyTextField;
        private VisualElement _choicesContainer;

        // Custom containers for vertical port placement
        private VisualElement _topPortContainer;
        private VisualElement _bottomPortContainer;

        private MasterDialogueData _data;
        private Action _onDataChanged;

        public DialogueNodeView(ConversationUnit unit, MasterDialogueData data, Action onDataChanged)
        {
            Unit = unit;
            _data = data;
            _onDataChanged = onDataChanged;

            AddToClassList("dialogue-node");

            // Set title to speaker name (not GUID)
            string displayTitle = !string.IsNullOrEmpty(unit.SpeakerName) ? unit.SpeakerName : "(No Speaker)";
            title = displayTitle;
            tooltip = $"ID:{unit.ID}\nEp:{unit.EpisodeID} Ch:{unit.ChapterID} Sec:{unit.SectionID}";

            // ---- Top Port (Input) ----
            _topPortContainer = new VisualElement();
            _topPortContainer.AddToClassList("top-port-container");

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "";
            InputPort.AddToClassList("top-port");
            _topPortContainer.Add(InputPort);

            // Insert at the very top of the node (before the title)
            Insert(0, _topPortContainer);

            // Hide default input/output containers (we use custom ones)
            inputContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;

            // ---- Bottom Port (Output / Next) ---- (must init before BuildContent because RebuildChoices uses it)
            _bottomPortContainer = new VisualElement();
            _bottomPortContainer.AddToClassList("bottom-port-container");

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "";
            OutputPort.AddToClassList("bottom-port");
            _bottomPortContainer.Add(OutputPort);

            // ---- Content ----
            BuildContent();

            // Add bottom port container after content
            Add(_bottomPortContainer);

            // ---- Appearance ----
            ApplyActorColor();

            // Position
            SetPosition(new Rect(unit.Position.x, unit.Position.y, 0, 0));

            RefreshExpandedState();
            RefreshPorts();
        }

        private void BuildContent()
        {
            var content = new VisualElement();
            content.style.paddingLeft = 4;
            content.style.paddingRight = 4;
            content.style.paddingTop = 2;
            content.style.paddingBottom = 4;

            // Speaker Dropdown
            var actorNames = _data.ActorDefinitions.Select(a => a.Name).ToList();
            if (actorNames.Count == 0) actorNames.Add("");

            string currentSpeaker = Unit.SpeakerName ?? "";
            if (!actorNames.Contains(currentSpeaker))
            {
                actorNames.Insert(0, currentSpeaker);
            }

            int currentIdx = actorNames.IndexOf(currentSpeaker);
            if (currentIdx < 0) currentIdx = 0;

            var speakerLabel = new Label("Speaker");
            speakerLabel.AddToClassList("node-speaker-label");
            content.Add(speakerLabel);

            _speakerDropdown = new PopupField<string>(actorNames, currentIdx);
            _speakerDropdown.AddToClassList("node-speaker-dropdown");
            _speakerDropdown.RegisterValueChangedCallback(evt =>
            {
                Unit.SpeakerName = evt.newValue;
                title = evt.newValue; // Update title to new speaker
                ApplyActorColor();
                _onDataChanged?.Invoke();
            });
            content.Add(_speakerDropdown);

            // Body Text
            _bodyTextField = new TextField();
            _bodyTextField.multiline = true;
            _bodyTextField.AddToClassList("node-body-text");
            _bodyTextField.value = Unit.BodyText ?? "";
            _bodyTextField.RegisterValueChangedCallback(evt =>
            {
                Unit.BodyText = evt.newValue;
                _onDataChanged?.Invoke();
            });
            content.Add(_bodyTextField);

            // Choices Header
            var choicesHeader = new VisualElement();
            choicesHeader.AddToClassList("choices-header");

            var choicesLabel = new Label("Choices");
            choicesLabel.AddToClassList("choices-label");
            choicesHeader.Add(choicesLabel);

            var addChoiceBtn = new Button(() => AddChoice()) { text = "+" };
            addChoiceBtn.AddToClassList("choice-add-btn");
            choicesHeader.Add(addChoiceBtn);

            content.Add(choicesHeader);

            // Choices Container
            _choicesContainer = new VisualElement();
            content.Add(_choicesContainer);

            extensionContainer.Add(content);

            // Build existing choices
            RebuildChoices();
        }

        private void AddChoice()
        {
            if (Unit.Choices == null) Unit.Choices = new List<Choice>();
            Unit.Choices.Add(new Choice { ChoiceText = "New Choice" });
            RebuildChoices();
            _onDataChanged?.Invoke();
        }

        public void RebuildChoices()
        {
            _choicesContainer.Clear();

            // Remove old choice ports from bottom container
            foreach (var p in _choicePorts)
            {
                // Disconnect edges
                foreach (var edge in p.connections.ToList())
                {
                    edge.input?.Disconnect(edge);
                    edge.output?.Disconnect(edge);
                    edge.RemoveFromHierarchy();
                }
                if (p.parent != null) p.parent.Remove(p);
            }
            _choicePorts.Clear();

            if (Unit.Choices == null) Unit.Choices = new List<Choice>();

            for (int i = 0; i < Unit.Choices.Count; i++)
            {
                int idx = i; // closure capture
                var choice = Unit.Choices[i];

                var row = new VisualElement();
                row.AddToClassList("choice-row");

                var choiceField = new TextField();
                choiceField.AddToClassList("choice-text-field");
                choiceField.value = choice.ChoiceText ?? "";
                choiceField.RegisterValueChangedCallback(evt =>
                {
                    choice.ChoiceText = evt.newValue;
                    _onDataChanged?.Invoke();
                });
                row.Add(choiceField);

                // Link / Jump button
                if (!string.IsNullOrEmpty(choice.TargetID))
                {
                    var linkBtn = new Button(() =>
                    {
                        // This will be handled by the graph view
                    })
                    { text = "->" };
                    linkBtn.AddToClassList("choice-link-btn");
                    row.Add(linkBtn);
                }

                // Delete
                var delBtn = new Button(() =>
                {
                    Unit.Choices.RemoveAt(idx);
                    RebuildChoices();
                    _onDataChanged?.Invoke();
                })
                { text = "x" };
                delBtn.AddToClassList("choice-delete-btn");
                row.Add(delBtn);

                _choicesContainer.Add(row);

                // Choice output port - added to bottom port container
                var choicePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                choicePort.portName = $"Choice {i}";
                choicePort.portColor = Color.cyan;
                _bottomPortContainer.Add(choicePort);
                _choicePorts.Add(choicePort);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        private void ApplyActorColor()
        {
            Color nodeColor = _data.GetActorColor(Unit.SpeakerName);
            var titleContainer = this.Q("title");
            if (titleContainer != null)
            {
                titleContainer.style.backgroundColor = new StyleColor(nodeColor);
                titleContainer.style.color = Color.white;
            }
        }

        /// <summary>
        /// Sync node position back to ConversationUnit after drag.
        /// </summary>
        public void SyncPositionToUnit()
        {
            var pos = GetPosition();
            Unit.Position = new Vector2(pos.x, pos.y);
        }
    }
}
