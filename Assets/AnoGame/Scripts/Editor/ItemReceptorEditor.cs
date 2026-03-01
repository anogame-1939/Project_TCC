using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using AnoGame.AnoFlow;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(ItemReceptor))]
    [CanEditMultipleObjects]
    public class ItemReceptorEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Script field (read-only)
            var scriptProp = serializedObject.FindProperty("m_Script");
            if (scriptProp != null)
            {
                var scriptField = new PropertyField(scriptProp);
                scriptField.SetEnabled(false);
                root.Add(scriptField);
            }

            // targetItemId ([ItemSelector] attribute → PropertyDrawer CreatePropertyGUI)
            var targetItemIdProp = serializedObject.FindProperty("targetItemId");
            if (targetItemIdProp != null)
            {
                var itemField = new PropertyField(targetItemIdProp, "Target Item ID");
                root.Add(itemField);
            }

            // Separator
            root.Add(CreateSeparator());

            // targetEventId ([EventSelector] attribute → PropertyDrawer CreatePropertyGUI)
            var targetEventIdProp = serializedObject.FindProperty("targetEventId");
            if (targetEventIdProp != null)
            {
                var eventField = new PropertyField(targetEventIdProp, "Target Event ID");
                root.Add(eventField);
            }

            // Separator
            root.Add(CreateSeparator());

            // ── Header: パラメータ ──
            var paramLabel = new Label("パラメータ");
            paramLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            paramLabel.style.fontSize = 13;
            paramLabel.style.marginTop = 8;
            paramLabel.style.marginBottom = 4;
            root.Add(paramLabel);

            // maxDistance
            var maxDistanceProp = serializedObject.FindProperty("maxDistance");
            if (maxDistanceProp != null)
            {
                root.Add(new PropertyField(maxDistanceProp, "Max Distance"));
            }

            // once
            var onceProp = serializedObject.FindProperty("once");
            if (onceProp != null)
            {
                root.Add(new PropertyField(onceProp, "Once"));
            }

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
