using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using AnoGame.AnoFlow;
using AnoGame.Application.Attributes;
using AnoGame.Data;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(InstantEventTrigger))]
    [CanEditMultipleObjects]
    public class InstantEventTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty scriptProp;
        private SerializedProperty targetEventIdProp;
        private SerializedProperty onStartProp;

        // EventTriggerBase propertes
        private SerializedProperty onPrepareEventProp;
        private SerializedProperty onEventStartProp;
        private SerializedProperty onEventFinishProp;
        private SerializedProperty onEventDoneProp;
        private SerializedProperty onEventFailedProp;

        private SerializedProperty supersededByEventsProp;
        private SerializedProperty conditionComponentsProp;
        private SerializedProperty requiredItemsProp;
        private SerializedProperty requiredEventsProp;
        private SerializedProperty checkConditionsOnDoneProp;

        private void OnEnable()
        {
            scriptProp = serializedObject.FindProperty("m_Script");
            targetEventIdProp = serializedObject.FindProperty("targetEventId");
            onStartProp = serializedObject.FindProperty("_onStart");

            onPrepareEventProp = serializedObject.FindProperty("onPrepareEvent");
            onEventStartProp = serializedObject.FindProperty("onEventStart");
            onEventFinishProp = serializedObject.FindProperty("onEventFinish");
            onEventDoneProp = serializedObject.FindProperty("onEventDone");
            onEventFailedProp = serializedObject.FindProperty("onEventFailed");

            supersededByEventsProp = serializedObject.FindProperty("supersededByEvents");
            conditionComponentsProp = serializedObject.FindProperty("conditionComponents");
            requiredItemsProp = serializedObject.FindProperty("requiredItems");
            requiredEventsProp = serializedObject.FindProperty("requiredEvents");
            checkConditionsOnDoneProp = serializedObject.FindProperty("checkConditionsOnDone");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Script field (read-only usually)
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(scriptProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Instant Event Logic (Fixed)", EditorStyles.boldLabel);

            // Check & Draw Target Event ID
            if (targetEventIdProp != null)
            {
                EditorGUILayout.PropertyField(targetEventIdProp, new GUIContent("Target Event ID"));
                // Fallback warning if ID is missing
                if (string.IsNullOrEmpty(targetEventIdProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Select an Event ID from the list.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Error: 'targetEventId' property not found!", MessageType.Error);
            }

            if (onStartProp != null) EditorGUILayout.PropertyField(onStartProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Condition Settings", EditorStyles.boldLabel);

            // Item Conditions
            DrawConditionList<ItemData>("Item Conditions", requiredItemsProp, "t:ItemData", "Add Item Condition", (item) => item.ItemName);

            // Event Conditions
            DrawConditionList<EventData>("Event Conditions", requiredEventsProp, "t:EventData", "Add Event Condition", (evt) => evt.EventName);

            EditorGUILayout.Space();

            // Legacy Foldout
            bool legacyExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(false, "Legacy Conditions");
            if (legacyExpanded)
            {
                // Keep foldout logic consistent
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Draw Legacy properties nicely
            EditorGUILayout.PropertyField(supersededByEventsProp);
            EditorGUILayout.PropertyField(conditionComponentsProp);
            if (checkConditionsOnDoneProp != null) EditorGUILayout.PropertyField(checkConditionsOnDoneProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            DrawProp(onPrepareEventProp);
            DrawProp(onEventStartProp);
            DrawProp(onEventFinishProp);
            DrawProp(onEventDoneProp);
            DrawProp(onEventFailedProp);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProp(SerializedProperty prop)
        {
            if (prop != null) EditorGUILayout.PropertyField(prop);
        }

        private void DrawConditionList<T>(string label, SerializedProperty listProp, string filter, string addButtonText, System.Func<T, string> nameSelector) where T : ScriptableObject
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (listProp == null) return;

            EditorGUI.indentLevel++;
            int count = listProp.arraySize;
            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);

                // Get referenced object
                T asset = elementProp.objectReferenceValue as T;
                string displayName = "None";
                if (asset != null)
                {
                    displayName = nameSelector(asset);
                    if (string.IsNullOrEmpty(displayName)) displayName = asset.name;
                }

                // Draw Read-only Label (or disabled text field)
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(displayName);
                }

                // Allow removing
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    // If reference exists, clear it first so DeleteArrayElementAtIndex actually removes the element
                    if (elementProp.objectReferenceValue != null)
                    {
                        elementProp.objectReferenceValue = null;
                    }
                    listProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            if (GUILayout.Button(addButtonText))
            {
                // Show dropdown
                var menu = new GenericMenu();
                string[] guids = AssetDatabase.FindAssets(filter);

                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        string name = nameSelector(asset);
                        if (string.IsNullOrEmpty(name)) name = asset.name;

                        menu.AddItem(new GUIContent(name), false, () =>
                        {
                            listProp.InsertArrayElementAtIndex(listProp.arraySize);
                            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = asset;
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
                menu.ShowAsContext();
            }
        }
    }
}
