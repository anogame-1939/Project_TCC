using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using AnoGame.AnoFlow;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(ContactReceptor))]
    [CanEditMultipleObjects]
    public class ContactReceptorEditor : UnityEditor.Editor
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
            root.Add(new PropertyField(serializedObject.FindProperty("targetEventId"), "Target Event ID"));
            root.Add(CreateSeparator());

            // Settings
            root.Add(new PropertyField(serializedObject.FindProperty("triggerDistance"), "Trigger Distance"));
            root.Add(new PropertyField(serializedObject.FindProperty("once"), "Once"));

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
