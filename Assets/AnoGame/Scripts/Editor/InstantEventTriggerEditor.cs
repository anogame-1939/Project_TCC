using UnityEngine;
using UnityEditor;
using AnoGame.Application.Event;
using AnoGame.Application.Attributes;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(InstantEventTrigger))]
    public class InstantEventTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetEventIdProp;
        private SerializedProperty _onStartProp;
        private SerializedProperty _onPrepareEventProp;
        private SerializedProperty _onEventStartProp;
        private SerializedProperty _onEventFinishProp;
        private SerializedProperty _onEventDoneProp;
        private SerializedProperty _onEventFailedProp;
        private SerializedProperty _conditionComponentsProp;
        private SerializedProperty _checkConditionsOnDoneProp;
        private SerializedProperty _supersededByEventsProp;

        private void OnEnable()
        {
            Debug.Log("[InstantEventTriggerEditor] OnEnable - Ver 2.0");
            _targetEventIdProp = serializedObject.FindProperty("targetEventId");
            _onStartProp = serializedObject.FindProperty("_onStart");

            _onPrepareEventProp = serializedObject.FindProperty("onPrepareEvent");
            _onEventStartProp = serializedObject.FindProperty("onEventStart");
            _onEventFinishProp = serializedObject.FindProperty("onEventFinish");
            _onEventDoneProp = serializedObject.FindProperty("onEventDone");
            _onEventFailedProp = serializedObject.FindProperty("onEventFailed");
            _conditionComponentsProp = serializedObject.FindProperty("conditionComponents");
            _checkConditionsOnDoneProp = serializedObject.FindProperty("checkConditionsOnDone");
            _supersededByEventsProp = serializedObject.FindProperty("supersededByEvents");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Custom Title
            EditorGUILayout.LabelField("Instant Event Logic (Fixed)", EditorStyles.boldLabel);

            // Check & Draw Target Event ID
            if (_targetEventIdProp != null)
            {
                EditorGUILayout.PropertyField(_targetEventIdProp, new GUIContent("Target Event ID"));
                // Fallback debug info if PropertyDrawer fails visually
                if (string.IsNullOrEmpty(_targetEventIdProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Select an Event ID from the list.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Error: 'targetEventId' property not found on InstantEventTrigger!", MessageType.Error);
                // Fallback to debug inspector
                DrawDefaultInspector();
                return;
            }

            if (_onStartProp != null) EditorGUILayout.PropertyField(_onStartProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Settings", EditorStyles.boldLabel);

            // Manual Draw to hide 'eventData'
            DrawProp(_onPrepareEventProp);
            DrawProp(_onEventStartProp);
            DrawProp(_onEventFinishProp);
            DrawProp(_onEventDoneProp);
            DrawProp(_onEventFailedProp);
            DrawProp(_conditionComponentsProp);
            DrawProp(_checkConditionsOnDoneProp);
            DrawProp(_supersededByEventsProp);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProp(SerializedProperty prop)
        {
            if (prop != null) EditorGUILayout.PropertyField(prop);
        }
    }
}
