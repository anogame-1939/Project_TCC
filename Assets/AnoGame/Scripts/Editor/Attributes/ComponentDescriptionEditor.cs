using System;
using UnityEditor;
using UnityEngine;
using AnoGame.Application.Direction; // For EncounterDirector

namespace AnoGame.Scripts.Editor.Attributes
{
    // Note: We need to explicitly target the class we want to use this editor for.
    // Since we can't easily make a global editor for MonoBehaviour without affecting everything,
    // we'll target specific classes or use a base class approach if needed later.
    // For now, per the plan, we target EncounterDirector.

    [CustomEditor(typeof(EnemyBehaviorCoordinator))]
    public class ComponentDescriptionEditor : UnityEditor.Editor
    {
        private string _description;

        private void OnEnable()
        {
            var attribute = (ComponentDescriptionAttribute)Attribute.GetCustomAttribute(target.GetType(), typeof(ComponentDescriptionAttribute));
            if (attribute != null)
            {
                _description = attribute.Description;
            }
        }

        public override void OnInspectorGUI()
        {
            if (!string.IsNullOrEmpty(_description))
            {
                EditorGUILayout.HelpBox(_description, MessageType.Info);
            }

            DrawDefaultInspector();
        }
    }
}
