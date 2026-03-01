using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using AnoGame.AnoFlow;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(ConsumeEventTrigger))]
    [CanEditMultipleObjects]
    public class ConsumeEventTriggerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Script
            var scriptProp = serializedObject.FindProperty("m_Script");
            if (scriptProp != null)
            {
                var f = new PropertyField(scriptProp);
                f.SetEnabled(false);
                root.Add(f);
            }

            // Info
            var infoBox = new HelpBox(
                "ItemReceptor 専用トリガー。OnConditionChanged による自動発火を行わず、" +
                "アイテム消費時にのみイベントが開始されます。",
                HelpBoxMessageType.Info);
            root.Add(infoBox);

            // Override
            var overrideLabel = new Label("Override");
            overrideLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            overrideLabel.style.marginTop = 6;
            overrideLabel.style.marginBottom = 4;
            root.Add(overrideLabel);

            root.Add(new PropertyField(serializedObject.FindProperty("targetEventId"), "Target Event ID"));
            root.Add(CreateSeparator());

            // EventTriggerBase fields
            AddEventTriggerBaseFields(root);

            return root;
        }

        private void AddEventTriggerBaseFields(VisualElement root)
        {
            // EventData
            root.Add(new PropertyField(serializedObject.FindProperty("eventData"), "Event Data"));
            root.Add(CreateSeparator());

            // Conditions
            var condLabel = new Label("Conditions");
            condLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            condLabel.style.marginTop = 6;
            condLabel.style.marginBottom = 4;
            root.Add(condLabel);

            root.Add(new PropertyField(serializedObject.FindProperty("requiredItems"), "Required Items"));
            root.Add(new PropertyField(serializedObject.FindProperty("requiredEvents"), "Required Events"));

            var ckProp = serializedObject.FindProperty("checkConditionsOnDone");
            if (ckProp != null) root.Add(new PropertyField(ckProp, "Check Conditions On Done"));
            root.Add(CreateSeparator());

            // Legacy
            var legacyLabel = new Label("Legacy");
            legacyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            legacyLabel.style.marginTop = 6;
            legacyLabel.style.marginBottom = 4;
            legacyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            root.Add(legacyLabel);

            root.Add(new PropertyField(serializedObject.FindProperty("supersededByEvents"), "Superseded By Events"));
            root.Add(new PropertyField(serializedObject.FindProperty("conditionComponents"), "Condition Components"));
            root.Add(CreateSeparator());

            // Events
            var evtLabel = new Label("Events");
            evtLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            evtLabel.style.marginTop = 6;
            evtLabel.style.marginBottom = 4;
            root.Add(evtLabel);

            root.Add(new PropertyField(serializedObject.FindProperty("onPrepareEvent"), "On Prepare"));
            root.Add(new PropertyField(serializedObject.FindProperty("onEventStart"), "On Event Start"));
            root.Add(new PropertyField(serializedObject.FindProperty("onEventFinish"), "On Event Finish"));
            root.Add(new PropertyField(serializedObject.FindProperty("onEventDone"), "On Event Done"));
            root.Add(new PropertyField(serializedObject.FindProperty("onEventFailed"), "On Event Failed"));
        }

        private VisualElement CreateSeparator()
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            sep.style.marginTop = 8;
            sep.style.marginBottom = 8;
            return sep;
        }
    }
}
