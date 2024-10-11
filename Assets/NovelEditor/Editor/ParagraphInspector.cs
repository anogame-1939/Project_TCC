using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using NovelEditor;
using UnityEngine.UIElements;
using System.Reflection;

namespace NovelEditor.Editor
{
    /// <summary>
    /// ParagraphNodeを選択した時のインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(TempParagraph))]
    internal class ParagraphInspector : UnityEditor.Editor
    {
        internal static TempParagraph editingData;
        TempParagraph tmpdata;
        private int index;

        VisualElement root;

        void OnEnable()
        {
            tmpdata = target as TempParagraph;
            editingData = tmpdata;
            SerializedProperty data = serializedObject.FindProperty(nameof(tmpdata.data));
            index = tmpdata.data.index;
        }

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            return Draw(root);
        }

        VisualElement Draw(VisualElement _root)
        {
            _root = new VisualElement();
            _root.styleSheets.Add(Resources.Load<StyleSheet>("DialogueUSS"));


            Label label = new Label();
            if (index == 0)
            {
                label.text = "最初に表示される会話です";
            }
            else
            {
                label.text = "！現在の立ち絵や背景に注意";
            }
            _root.Add(label);

            var nameText = new IMGUIContainer(() =>
            {
                serializedObject.Update();

                SerializedProperty nodeName = serializedObject.FindProperty("data").FindPropertyRelative("nodeName");
                nodeName.stringValue = EditorGUILayout.TextField("ノードの名前(必要な場合のみ)", nodeName.stringValue);
                serializedObject.ApplyModifiedProperties(); serializedObject.ApplyModifiedProperties();

            });

            _root.Add(nameText);

            var list = new ListView();
            list.reorderable = true;
            list.showBorder = true;
            list.showAddRemoveFooter = true;
            list.bindingPath = "dialogueList";
            list.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            list.itemIndexChanged += (index1, index2) =>
            {
                tmpdata.data.UpdateOrder(NovelEditorWindow.editingData);
                EditorUtility.SetDirty(NovelEditorWindow.editingData);
            };
            list.itemsAdded += (a) =>
            {
                tmpdata.data.UpdateOrder(NovelEditorWindow.editingData);
                EditorUtility.SetDirty(NovelEditorWindow.editingData);
            };

            list.itemsRemoved += (a) =>
            {
                tmpdata.data.UpdateOrder(NovelEditorWindow.editingData);
                EditorUtility.SetDirty(NovelEditorWindow.editingData);
            };

            _root.Add(list);

            return _root;
        }

        internal static void UpdateValue()
        {
            editingData.data.UpdateOrder(NovelEditorWindow.editingData);
            EditorUtility.SetDirty(NovelEditorWindow.editingData);
        }

    }
}