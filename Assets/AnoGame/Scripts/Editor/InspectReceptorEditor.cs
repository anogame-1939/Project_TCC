using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using AnoGame.AnoFlow;
using AnoGame.AnoFlow.Editor;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(InspectReceptor))]
    [CanEditMultipleObjects]
    public class InspectReceptorEditor : UnityEditor.Editor
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

            // Event
            var eventField = new PropertyField(serializedObject.FindProperty("targetEventId"), "Target Event ID");
            ReceptorEditorUtility.SetupDuplicateCheck(
                eventField, serializedObject, "targetEventId", (Component)target);
            root.Add(eventField);
            root.Add(CreateSeparator());

            // Interaction Settings
            root.Add(new PropertyField(serializedObject.FindProperty("prompt"), "Prompt"));
            root.Add(new PropertyField(serializedObject.FindProperty("maxDistance"), "Max Distance"));
            root.Add(new PropertyField(serializedObject.FindProperty("priority"), "Priority"));
            root.Add(new PropertyField(serializedObject.FindProperty("once"), "Once"));
            root.Add(CreateSeparator());

            // Reference
            root.Add(new PropertyField(serializedObject.FindProperty("uiAnchor"), "UI Anchor"));

            return root;
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
