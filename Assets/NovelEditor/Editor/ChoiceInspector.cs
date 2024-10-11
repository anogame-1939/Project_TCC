using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NovelEditor.Editor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// 選択肢のノードを表示した時のインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(TempChoice))]
    internal class ChoiceInspector : UnityEditor.Editor
    {
        TempChoice tmpdata;

        void OnEnable()
        {
            tmpdata = target as TempChoice;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty data = serializedObject.FindProperty("data");
            SerializedProperty text = data.FindPropertyRelative("text");
            SerializedProperty nodeName = data.FindPropertyRelative("nodeName");

            text.stringValue = EditorGUILayout.TextField("選択肢のテキスト", text.stringValue);
            nodeName.stringValue = EditorGUILayout.TextField("ノードの名前(必要な場合のみ)", nodeName.stringValue);

            serializedObject.ApplyModifiedProperties();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            Label label = new Label();

            var container = new IMGUIContainer(OnInspectorGUI);
            root.Add(container);

            return root;
        }
    }
}