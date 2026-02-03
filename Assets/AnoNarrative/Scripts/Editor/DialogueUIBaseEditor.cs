using UnityEngine;
using UnityEditor;
using AnoGame.AnoNarrative.UI;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.AnoNarrative.Editor
{
    [CustomEditor(typeof(DialogueUIBase), true)]
    public class DialogueUIBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _styleData;
        private SerializedProperty _styleName;

        private void OnEnable()
        {
            _styleData = serializedObject.FindProperty("styleData");
            _styleName = serializedObject.FindProperty("styleName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw Default Script field
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            EditorGUILayout.PropertyField(prop); // Script

            // Draw Style Settings
            if (_styleData != null && _styleName != null)
            {
                EditorGUILayout.PropertyField(_styleData, new GUIContent("Style Database"));

                var db = (AnoGame.AnoNarrative.Data.DialogueStyle)_styleData.objectReferenceValue;

                if (db == null)
                {
                    // Try auto-assign
                    string[] guids = AssetDatabase.FindAssets("t:DialogueStyle");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        db = AssetDatabase.LoadAssetAtPath<AnoGame.AnoNarrative.Data.DialogueStyle>(path);
                        if (db != null)
                        {
                            _styleData.objectReferenceValue = db;
                        }
                    }
                    if (GUILayout.Button("Find Database Automatically"))
                    {
                        guids = AssetDatabase.FindAssets("t:DialogueStyle");
                        if (guids.Length > 0)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            _styleData.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnoGame.AnoNarrative.Data.DialogueStyle>(path);
                        }
                    }
                }

                if (db != null)
                {
                    if (db.styleNames != null && db.styleNames.Count > 0)
                    {
                        var list = db.styleNames.ToList();
                        int index = list.IndexOf(_styleName.stringValue);
                        if (index < 0) index = 0;

                        int newIndex = EditorGUILayout.Popup("Style Name", index, list.ToArray());
                        if (newIndex >= 0 && newIndex < list.Count)
                        {
                            _styleName.stringValue = list[newIndex];
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Database has no styles defined.");
                        EditorGUILayout.PropertyField(_styleName);
                    }
                }
                else
                {
                    // No DB, allow manual entry
                    EditorGUILayout.PropertyField(_styleName);
                }
            }

            // Draw remaining properties (skipping already drawn ones)
            // We started iteration. Let's finish it.
            // Actually, simplest way to draw 'rest' of inspector is to loop.
            // But since we want to handle specific fields, let's just do DrawPropertiesExcluding.

            DrawPropertiesExcluding(serializedObject, "m_Script", "styleData", "styleName");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
