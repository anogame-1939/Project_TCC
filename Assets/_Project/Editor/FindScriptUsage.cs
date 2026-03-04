// Assets/AnoGame/Editor/FindScriptUsage_Window.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnoGame
{
    /// <summary>
    /// Projectビューでスクリプトを右クリック → シーンを一時オープンして型検索 → 結果をウィンドウ表示
    /// 行クリックで：すでに開いていればPingのみ、未オープンならSingle/Additiveを自動選択して開く
    /// </summary>
    public static class FindScriptUsage
    {
        // 🔎 検索対象フォルダ（サブフォルダも再帰検索）
        private static readonly string[] SEARCH_FOLDERS = { "Assets/AnoGame/Scenes" };

        [MenuItem("Assets/Find Usage ▶ In Scenes (Scan & Window)", true)]
        private static bool Validate_Run()
        {
            var ms = Selection.activeObject as MonoScript;
            var t  = ms != null ? ms.GetClass() : null;
            return t != null && typeof(Component).IsAssignableFrom(t);
        }

        [MenuItem("Assets/Find Usage ▶ In Scenes (Scan & Window)")]
        private static void Run()
        {
            var ms = (MonoScript)Selection.activeObject;
            var targetType = ms.GetClass();
            if (targetType == null || !typeof(Component).IsAssignableFrom(targetType))
            {
                Debug.LogError("対象は MonoBehaviour/Component 派生クラスのみです。");
                return;
            }

            var setup   = EditorSceneManager.GetSceneManagerSetup(); // 復元用
            var folders = SEARCH_FOLDERS.Where(AssetDatabase.IsValidFolder).ToArray();

            var results = new List<FindScriptUsageWindow.ResultRow>();
            var sceneGuids = (folders.Length > 0)
                ? AssetDatabase.FindAssets("t:Scene", folders)
                : AssetDatabase.FindAssets("t:Scene");

            try
            {
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (EditorUtility.DisplayCancelableProgressBar("Scanning Scenes", scenePath, (float)i / sceneGuids.Length))
                        break;

                    // シーンを一時的に開いて検索（変更はしない）
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    var objects = GameObject.FindObjectsByType(
                        targetType,
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                    foreach (var obj in objects)
                    {
                        if (obj is Component comp && comp.gameObject.scene == scene)
                        {
                            var path = BuildHierarchyPath(comp.gameObject);
                            results.Add(new FindScriptUsageWindow.ResultRow
                            {
                                ScenePath = scenePath,
                                SceneFile = System.IO.Path.GetFileName(scenePath),
                                Hierarchy = path.Split('/'),
                                DisplayPath = path,
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // 走査後に元のシーン構成へ復元
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            // 結果ウィンドウを開いて一覧表示
            FindScriptUsageWindow.Open(
                targetTypeName: ms.GetClass().FullName,
                headerFolder:   SEARCH_FOLDERS.FirstOrDefault() ?? "Assets/",
                rows:           results
            );
        }

        private static string BuildHierarchyPath(GameObject go)
        {
            var t = go.transform;
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }
    }

    public class FindScriptUsageWindow : EditorWindow
    {
        public class ResultRow
        {
            public string ScenePath;   // Assets/.../X.unity
            public string SceneFile;   // X.unity
            public string[] Hierarchy; // ["Root","Child","Target"]
            public string DisplayPath; // Root/Child/Target
        }

        private static List<ResultRow> _rows = new();
        private static string _headerFolder = "Assets/";
        private static string _targetTypeName = "";
        private Vector2 _scroll;
        private string _filter = "";

        public static void Open(string targetTypeName, string headerFolder, List<ResultRow> rows)
        {
            _targetTypeName = targetTypeName ?? "";
            _headerFolder   = headerFolder ?? "Assets/";
            _rows           = rows ?? new List<ResultRow>();

            var wnd = GetWindow<FindScriptUsageWindow>("Find Script Usage");
            wnd.minSize = new Vector2(560, 340);
            wnd.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField($"Type: {_targetTypeName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_headerFolder, EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Filter (path contains)", GUILayout.Width(150));
                _filter = EditorGUILayout.TextField(_filter);
                if (GUILayout.Button("Copy All", GUILayout.Width(90)))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(_headerFolder);
                    foreach (var row in GetFilteredRows())
                        sb.AppendLine($"{row.SceneFile} -> {row.DisplayPath}");
                    GUIUtility.systemCopyBuffer = sb.ToString();
                    ShowNotification(new GUIContent("Copied to clipboard"));
                }
            }

            EditorGUILayout.Space(6);
            DrawList();
        }

        private IEnumerable<ResultRow> GetFilteredRows()
        {
            if (string.IsNullOrEmpty(_filter))
                return _rows.OrderBy(r => r.SceneFile).ThenBy(r => r.DisplayPath);

            var f = _filter.Trim();
            return _rows.Where(r =>
                       r.SceneFile.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       r.DisplayPath.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                       .OrderBy(r => r.SceneFile)
                       .ThenBy(r => r.DisplayPath);
        }

        private void DrawList()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                foreach (var row in GetFilteredRows())
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        // 行クリックで Open & Ping
                        if (GUILayout.Button($"{row.SceneFile} -> {row.DisplayPath}", EditorStyles.label, GUILayout.ExpandWidth(true)))
                        {
                            OpenSceneAndPing(row);
                        }

                        if (GUILayout.Button("Open & Ping", GUILayout.Width(110)))
                        {
                            OpenSceneAndPing(row);
                        }
                    }
                }
            }
        }

        // ==== ここから：要求どおりのアダプティブなオープン＋Ping ====

        private static bool IsSceneOpen(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && s.isLoaded) return true;
            }
            return false;
        }

        private static Scene GetOpenSceneByPath(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && s.isLoaded) return s;
            }
            return default;
        }

        private static bool HasAnyOpenScene()
        {
            return SceneManager.sceneCount > 0 &&
                   Enumerable.Range(0, SceneManager.sceneCount)
                             .Select(SceneManager.GetSceneAt)
                             .Any(s => s.isLoaded);
        }

        private void OpenSceneAndPing(ResultRow row)
        {
            Scene scene;

            if (IsSceneOpen(row.ScenePath))
            {
                // 既に開いている：開き直さない
                scene = GetOpenSceneByPath(row.ScenePath);
                if (scene.IsValid())
                {
                    // 操作しやすいようアクティブに
                    EditorSceneManager.SetActiveScene(scene);
                }
            }
            else
            {
                // 未オープン：アダプティブに開く
                // 何も開いていなければ Single、既に何か開いていれば Additive
                var mode = HasAnyOpenScene() ? OpenSceneMode.Additive : OpenSceneMode.Single;

                // （未保存変更がある場合は Unity がダイアログで保護します）
                scene = EditorSceneManager.OpenScene(row.ScenePath, mode);

                // Additive で開いた場合も操作性のためアクティブにしておく
                if (scene.IsValid())
                    EditorSceneManager.SetActiveScene(scene);
            }

            // ルートから順にたどって対象を探す
            var go = FindByHierarchy(scene, row.Hierarchy);
            if (go != null)
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Not Found",
                    "シーン構造が更新され、保存時のパスと一致しない可能性があります。\n再スキャンしてください。",
                    "OK"
                );
            }
        }

        private GameObject FindByHierarchy(Scene scene, string[] hierarchy)
        {
            if (hierarchy == null || hierarchy.Length == 0) return null;

            // まずroot一致を探す
            GameObject current = scene.GetRootGameObjects().FirstOrDefault(g => g.name == hierarchy[0]);
            if (current == null) return null;

            if (hierarchy.Length == 1) return current;

            // 子階層は Transform.Find でスラッシュ結合して探索
            var subPath = string.Join("/", hierarchy.Skip(1));
            var t = current.transform.Find(subPath);
            return t != null ? t.gameObject : null;
        }
    }
}
#endif
