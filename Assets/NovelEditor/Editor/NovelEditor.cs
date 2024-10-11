using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using NovelEditor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// NovelEditorを制御するためのクラス
    /// </summary>
    public class NovelEditor
    {
        /// <summary>
        /// NovelEditorWindowを開き、データを保存できるようにする
        /// </summary>
        /// <param name="data">編集したいデータ</param>
        public static void Open(NovelData data)
        {
            //ウィンドウ作成
            var scene = typeof(UnityEditor.SceneView);
            var window = EditorWindow.GetWindow<NovelEditorWindow>(title: "NovelEditor");
            window.Close();
            window = EditorWindow.GetWindow<NovelEditorWindow>(desiredDockNextTo: new Type[] { scene }, title: "NovelEditor");
            window.Init(data);
        }

        /// <summary>
        /// メニューからウィンドウを開くためのメソッド
        /// </summary>
        [MenuItem("Tool/NovelEditor")]
        public static void Open()
        {
            //ウィンドウ作成
            var scene = typeof(UnityEditor.SceneView);
            var window = EditorWindow.GetWindow<NovelEditorWindow>(title: "NovelEditor");
            window.Init(null);
        }
    }
}