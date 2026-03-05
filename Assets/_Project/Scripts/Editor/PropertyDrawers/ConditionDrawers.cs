using UnityEngine;
using UnityEditor;
using AnoGame.Data;

namespace AnoGame.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ItemCondition))]
    public class ItemConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Find the inner 'itemId' property
            SerializedProperty itemIdProp = property.FindPropertyRelative("itemId");
            if (itemIdProp != null)
            {
                // Draw the inner property directly, effectively skipping the wrapper foldout.
                // We use the original label (e.g., "Element 0") but apply it to the inner drawer.
                EditorGUI.PropertyField(position, itemIdProp, label);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Error: itemId not found");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty itemIdProp = property.FindPropertyRelative("itemId");
            return itemIdProp != null
                ? EditorGUI.GetPropertyHeight(itemIdProp, label)
                : EditorGUIUtility.singleLineHeight;
        }
    }

    [CustomPropertyDrawer(typeof(EventCondition))]
    public class EventConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty eventIdProp = property.FindPropertyRelative("eventId");
            if (eventIdProp != null)
            {
                EditorGUI.PropertyField(position, eventIdProp, label);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Error: eventId not found");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty eventIdProp = property.FindPropertyRelative("eventId");
            return eventIdProp != null
                ? EditorGUI.GetPropertyHeight(eventIdProp, label)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}
