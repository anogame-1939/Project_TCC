using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnoGame.EditorExtensions
{
    public class UtilityLauncherWindow : EditorWindow
    {
        private struct UtilityTool
        {
            public string Name;
            public Action Action;
            public string Description;

            public UtilityTool(string name, Action action, string description = "")
            {
                Name = name;
                Action = action;
                Description = description;
            }
        }

        private List<UtilityTool> _tools;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Utility Launcher")]
        public static void ShowWindow()
        {
            GetWindow<UtilityLauncherWindow>("Utility Launcher");
        }

        private void OnEnable()
        {
            InitializeTools();
        }

        private void InitializeTools()
        {
            _tools = new List<UtilityTool>
            {
                new UtilityTool(
                    "オブジェクトピン留め (Object Pinger)",
                    () => EditorApplication.ExecuteMenuItem("Tools/Object Pinger"),
                    "頻繁に使うオブジェクトをリスト化してすぐに選択・Pingできるウィンドウを開きます。"
                ),
                new UtilityTool(
                    "保存データフォルダを開く (Open Save Data)",
                    () => EditorUtility.RevealInFinder(UnityEngine.Application.persistentDataPath),
                    "Application.persistentDataPath をエクスプローラーで開きます。"
                ),
                new UtilityTool(
                    "PlayerPrefs削除 (Delete All PlayerPrefs)",
                    DeleteAllPlayerPrefs,
                    "全てのPlayerPrefsを削除します。実行前に確認ダイアログが出ます。"
                )
            };
        }

        private void DeleteAllPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog("PlayerPrefs削除",
                "本当に全てのPlayerPrefsを削除しますか？\nこの操作は取り消せません。",
                "削除する", "キャンセル"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("PlayerPrefs have been deleted.");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("便利ツール一覧", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var tool in _tools)
            {
                DrawToolButton(tool);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolButton(UtilityTool tool)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button(tool.Name, GUILayout.Height(30)))
            {
                tool.Action?.Invoke();
            }

            if (!string.IsNullOrEmpty(tool.Description))
            {
                EditorGUILayout.LabelField(tool.Description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}
