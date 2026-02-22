using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using AnoGame.AnoDialogue.Timeline;

namespace AnoGame.AnoDialogue.Editor
{
    [CustomEditor(typeof(SceneImageClip))]
    public class SceneImageClipEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // ── Header ──
            var headerLabel = new Label("Scene Image Clip");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 13;
            headerLabel.style.marginTop = 4;
            headerLabel.style.marginBottom = 6;
            root.Add(headerLabel);

            // ── Scene Image ──
            var sceneImageProp = serializedObject.FindProperty("sceneImage");
            if (sceneImageProp != null)
            {
                var imgField = new PropertyField(sceneImageProp, "Scene Image");
                root.Add(imgField);
            }

            // ── Style Database ──
            var styleDbProp = serializedObject.FindProperty("styleDatabase");
            if (styleDbProp != null)
            {
                var dbField = new PropertyField(styleDbProp, "Style Database");
                root.Add(dbField);

                // Auto-assign if empty
                if (styleDbProp.objectReferenceValue == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:DialogueStyle");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var db = AssetDatabase.LoadAssetAtPath<AnoGame.AnoDialogue.Data.DialogueStyle>(path);
                        if (db != null)
                        {
                            styleDbProp.objectReferenceValue = db;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }

                // ── Dialogue Style Dropdown ──
                var styleContainer = new VisualElement();
                styleContainer.name = "style-dropdown-container";
                root.Add(styleContainer);

                dbField.RegisterValueChangeCallback(evt =>
                {
                    RebuildStyleDropdown(styleContainer);
                });

                styleContainer.schedule.Execute(() => RebuildStyleDropdown(styleContainer));
            }

            return root;
        }

        private void RebuildStyleDropdown(VisualElement container)
        {
            container.Clear();

            var styleDbProp = serializedObject.FindProperty("styleDatabase");
            var styleNameProp = serializedObject.FindProperty("dialogueStyleName");
            if (styleDbProp == null || styleNameProp == null) return;

            var db = styleDbProp.objectReferenceValue as AnoGame.AnoDialogue.Data.DialogueStyle;
            if (db == null || db.styleNames == null || db.styleNames.Count == 0)
            {
                if (db != null)
                {
                    container.Add(new HelpBox("Database has no styles defined.", HelpBoxMessageType.Info));
                }
                return;
            }

            var list = db.styleNames.ToList();
            int index = list.IndexOf(styleNameProp.stringValue);
            if (index < 0)
            {
                index = 0;
                // 初期値が空または未登録の場合、最初のスタイルを自動設定
                styleNameProp.stringValue = list[0];
                serializedObject.ApplyModifiedProperties();
            }

            var dropdown = new DropdownField("Target Style", list, index);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                styleNameProp.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            container.Add(dropdown);
        }
    }
}
