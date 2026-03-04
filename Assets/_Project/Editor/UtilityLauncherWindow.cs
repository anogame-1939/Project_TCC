using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using AnoGame.SLFBDebug;
using AnoGame.Scripts.Editor; // For ObjectMoverWindow

namespace AnoGame.EditorExtensions
{
    public class UtilityLauncherWindow : EditorWindow
    {
        private class UtilityTool
        {
            public string ID; // Unique ID for persistence
            public string Name;
            public Action Action;
            public string Description;

            public UtilityTool(string id, string name, Action action, string description = "")
            {
                ID = id;
                Name = name;
                Action = action;
                Description = description;
            }
        }

        [System.Serializable]
        private class ToolOrderData
        {
            public List<string> order = new List<string>();
        }

        private List<UtilityTool> _tools;
        private ReorderableList _reorderableList;
        private Vector2 _scrollPosition;
        private const string PREFS_KEY = "UtilityLauncher_ToolOrder_V2";

        [MenuItem("AnoGame/Utility Launcher")]
        public static void ShowWindow()
        {
            GetWindow<UtilityLauncherWindow>("Utility Launcher");
        }

        private void OnEnable()
        {
            InitializeTools();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SaveOrder();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            Repaint();
        }

        private void InitializeTools()
        {
            // Define all available tools here
            // Note: ObjectPingerWindow is accessed via Type name since it is in same namespace/assembly accessible
            // ObjectMoverWindow and ObjectTogglerWindow are in AnoGame.Scripts.Editor namespace.

            var allTools = new List<UtilityTool>
            {
                new UtilityTool(
                    "Mover",
                    "オブジェクト移動 (Object Mover)",
                    () => ObjectMoverWindow.ShowWindow(),
                    "WASDキーでオブジェクトを移動させるツールを開きます。"
                ),
                new UtilityTool(
                    "Toggler",
                    "オブジェクト表示切替 (Object Toggler)",
                    () => ObjectTogglerWindow.ShowWindow(),
                    "オブジェクトの表示/非表示をリスト管理できるツールを開きます。"
                ),
                new UtilityTool(
                    "Pinger",
                    "オブジェクトピン留め (Object Pinger)",
                    () => ObjectPingerWindow.ShowWindow(),
                    "頻繁に使うオブジェクトをリスト化してすぐに選択・Pingできるウィンドウを開きます。"
                ),
                new UtilityTool(
                    "OpenSaveData",
                    "保存データフォルダを開く (Open Save Data)",
                    () => EditorUtility.RevealInFinder(UnityEngine.Application.persistentDataPath),
                    "Application.persistentDataPath をエクスプローラーで開きます。"
                ),
                new UtilityTool(
                    "DeletePrefs",
                    "PlayerPrefs削除 (Delete All PlayerPrefs)",
                    DeleteAllPlayerPrefs,
                    "全てのPlayerPrefsを削除します。実行前に確認ダイアログが出ます。"
                ),
                new UtilityTool(
                    "SaveDataSwitcher",
                    "Save Data Switcher",
                    () => SavedataSwitcherWindow.ShowWindow(),
                    "セーブデータの切り替え・バックアップ・作成を行うウィンドウを開きます。"
                ),
                new UtilityTool(
                    "ItemEditor",
                    "Item Editor",
                    () => ItemEditorWindow.ShowWindow(),
                    "アイテムデータの作成・編集を行います。"
                ),
                new UtilityTool(
                    "EventEditor",
                    "Event Editor",
                    () => EventEditorWindow.ShowWindow(),
                    "イベントデータの作成・並べ替えを行います。"
                )
            };

            // Load saved order and sort
            _tools = new List<UtilityTool>();
            if (EditorPrefs.HasKey(PREFS_KEY))
            {
                string json = EditorPrefs.GetString(PREFS_KEY);
                var savedOrder = JsonUtility.FromJson<ToolOrderData>(json);

                if (savedOrder != null && savedOrder.order != null)
                {
                    // Add tools in saved order
                    foreach (var id in savedOrder.order)
                    {
                        var tool = allTools.FirstOrDefault(t => t.ID == id);
                        if (tool != null)
                        {
                            _tools.Add(tool);
                            allTools.Remove(tool);
                        }
                    }
                }
            }

            // Append any remaining new tools (that were not in saved order)
            _tools.AddRange(allTools);

            InitializeReorderableList();
        }

        private void InitializeReorderableList()
        {
            _reorderableList = new ReorderableList(_tools, typeof(UtilityTool), true, false, false, false);

            _reorderableList.drawHeaderCallback = null;
            _reorderableList.footerHeight = 0;

            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= _tools.Count) return;
                var tool = _tools[index];

                rect.y += 2;
                rect.height -= 4;

                // Visual background for item
                GUI.Box(rect, "", GUI.skin.box);

                Rect btnRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 30);
                if (GUI.Button(btnRect, tool.Name))
                {
                    tool.Action?.Invoke();
                }

                if (!string.IsNullOrEmpty(tool.Description))
                {
                    Rect descRect = new Rect(rect.x + 4, rect.y + 36, rect.width - 8, rect.height - 36);
                    EditorGUI.LabelField(descRect, tool.Description, EditorStyles.wordWrappedMiniLabel);
                }
            };

            _reorderableList.elementHeightCallback = (int index) =>
            {
                if (index < 0 || index >= _tools.Count) return 0;
                var tool = _tools[index];

                float height = 40; // Base height for button + padding
                if (!string.IsNullOrEmpty(tool.Description))
                {
                    float contentWidth = EditorGUIUtility.currentViewWidth - 60;
                    float labelHeight = EditorStyles.wordWrappedMiniLabel.CalcHeight(new GUIContent(tool.Description), contentWidth);
                    height += labelHeight + 5;
                }
                return height + 8; // Extra padding
            };

            _reorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
            {
                SaveOrder();
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

        private void SaveOrder()
        {
            var data = new ToolOrderData();
            data.order = _tools.Select(t => t.ID).ToList();
            string json = JsonUtility.ToJson(data);
            EditorPrefs.SetString(PREFS_KEY, json);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("便利ツール一覧 (ドラッグで並べ替え)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_reorderableList != null)
            {
                _reorderableList.DoLayoutList();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("並び順をリセット (Reset Order)"))
            {
                EditorPrefs.DeleteKey(PREFS_KEY);
                InitializeTools();
                Repaint();
            }
        }
    }
}
