#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

using AnoGame.Data; // EventData の名前空間

public sealed class EventEditorWindow : EditorWindow
{
    // ───────── 設定（既定保存先は const） ─────────
    private const string DEFAULT_SAVE_PATH = "Assets/AnoGame/Data/Events";
    private const string EDITORPREFS_PATH_KEY = "AnoGame.EventEditor.SavePath";

    // ファイル名書式: 1-2.013.イベント名.asset
    private static readonly Regex NameRegex =
        new Regex(@"^(\d+)-(\d+)\.(\d{3})\.(.+)$", RegexOptions.Compiled);

    // ───────── GUI 状態 ─────────
    private EventData _draft;              // 画面で編集するドラフト
    private SerializedObject _so;
    private SerializedProperty _spEventId;
    private SerializedProperty _spEventName;
    private SerializedProperty _spDescription;
    private SerializedProperty _spIsOneTime;

    private static readonly int[] ChapterOptions = Enumerable.Range(1, 50).ToArray();
    private static readonly int[] SectionOptions = Enumerable.Range(1, 50).ToArray();
    private int _chapterIndex; // 0-based index for arrays above
    private int _sectionIndex;

    private string _savePath;

    [MenuItem("Tools/EventEditor")]
    private static void Open()
    {
        var w = GetWindow<EventEditorWindow>("Event Editor");
        w.minSize = new Vector2(480, 420);
        w.Show();
    }

    private void OnEnable()
    {
        if (_draft == null) _draft = ScriptableObject.CreateInstance<EventData>();
        _so = new SerializedObject(_draft);
        _spEventId     = _so.FindProperty("eventId");
        _spEventName   = _so.FindProperty("eventName");
        _spDescription = _so.FindProperty("description");
        _spIsOneTime   = _so.FindProperty("isOneTime");

        _savePath = EditorPrefs.GetString(EDITORPREFS_PATH_KEY, DEFAULT_SAVE_PATH);

        _chapterIndex = 0; // 1章
        _sectionIndex = 0; // 1節
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(_savePath))
            EditorPrefs.SetString(EDITORPREFS_PATH_KEY, _savePath);
        if (_draft != null) DestroyImmediate(_draft);
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("章 / 節", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _chapterIndex = EditorGUILayout.Popup(
                    new GUIContent("章"),
                    _chapterIndex,
                    ChapterOptions.Select(i => i.ToString()).ToArray(),
                    GUILayout.MaxWidth(240));

                _sectionIndex = EditorGUILayout.Popup(
                    new GUIContent("節"),
                    _sectionIndex,
                    SectionOptions.Select(i => i.ToString()).ToArray(),
                    GUILayout.MaxWidth(240));
            }
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("EventData プロパティ", EditorStyles.boldLabel);
            _so.Update();
            EditorGUILayout.PropertyField(_spEventId,     new GUIContent("Event Id"));
            EditorGUILayout.PropertyField(_spEventName,   new GUIContent("Event Name"));
            EditorGUILayout.PropertyField(_spDescription, new GUIContent("Description"), GUILayout.MinHeight(48));
            EditorGUILayout.PropertyField(_spIsOneTime,   new GUIContent("Is One Time"));
            _so.ApplyModifiedProperties();
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("保存設定", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("保存先 (相対)", GUILayout.Width(120));
                _savePath = EditorGUILayout.TextField(_savePath);
                if (GUILayout.Button("選択...", GUILayout.Width(80)))
                {
                    var selected = EditorUtility.OpenFolderPanel("保存先フォルダを選択", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        var proj = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        if (selected.StartsWith(proj))
                        {
                            var rel = "Assets" + selected.Substring(proj.Length).Replace("\\", "/");
                            _savePath = rel;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("無効なパス", "プロジェクト内（Assets配下）のフォルダを選択してください。", "OK");
                        }
                    }
                }
                if (GUILayout.Button("既定に戻す", GUILayout.Width(100)))
                {
                    _savePath = DEFAULT_SAVE_PATH;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存（作成）", GUILayout.Height(26)))
                    SaveEvent();

                if (GUILayout.Button("枝番の振り直し（章‐節）", GUILayout.Height(26)))
                    RenumberBranchesForCurrentChapterSection();
            }
        }
    }

    // ───────── 保存 ─────────
    private void SaveEvent()
    {
        string evName = (_spEventName.stringValue ?? "").Trim();
        if (string.IsNullOrEmpty(evName))
        {
            EditorUtility.DisplayDialog("エラー", "Event Name を入力してください。", "OK");
            return;
        }

        // フォルダ作成
        if (!AssetDatabase.IsValidFolder(_savePath))
        {
            Directory.CreateDirectory(_savePath);
            AssetDatabase.Refresh();
        }

        int chapter = ChapterOptions[_chapterIndex];
        int section = SectionOptions[_sectionIndex];

        // 既存資産から同じ 章-節 の最大枝番を検索
        int next = FindNextBranchNumber(_savePath, chapter, section);

        string safeName = SanitizeFileName(evName);
        string fileNameNoExt = $"{chapter}-{section}.{next:000}.{safeName}";
        string path = $"{_savePath}/{fileNameNoExt}.asset";

        // 新規アセットを作成（ドラフト内容をコピー）
        var newAsset = ScriptableObject.CreateInstance<EventData>();
        EditorUtility.CopySerialized(_draft, newAsset);

        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(newAsset);

        Debug.Log($"[EventEditor] 生成: {path}");
    }

    // ───────── 振り直し ─────────
    private void RenumberBranchesForCurrentChapterSection()
    {
        int chapter = ChapterOptions[_chapterIndex];
        int section = SectionOptions[_sectionIndex];

        if (!AssetDatabase.IsValidFolder(_savePath))
        {
            EditorUtility.DisplayDialog("情報", "保存先フォルダがまだ存在しません。", "OK");
            return;
        }

        // 指定 章-節 の EventData アセットを収集
        var guids = AssetDatabase.FindAssets("t:EventData", new[] { _savePath });
        var targets = new List<(string guid, string path, string namePart)>();

        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var name = Path.GetFileNameWithoutExtension(p);
            var m = NameRegex.Match(name);
            if (!m.Success) continue;

            int ch = int.Parse(m.Groups[1].Value);
            int se = int.Parse(m.Groups[2].Value);
            if (ch != chapter || se != section) continue;

            string nm = m.Groups[4].Value; // イベント名
            targets.Add((g, p, nm));
        }

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("情報", $"章{chapter}-節{section} に該当アセットが見つかりません。", "OK");
            return;
        }

        // イベント名（日本語含む）で昇順に並べ、001,002... を付け直す
        targets = targets
            .OrderBy(t => t.namePart, StringComparer.CurrentCulture)
            .ToList();

        int branch = 1;
        foreach (var t in targets)
        {
            string dir = Path.GetDirectoryName(t.path).Replace("\\", "/");
            string newName = $"{chapter}-{section}.{branch:000}.{t.namePart}";
            string newPath = $"{dir}/{newName}.asset";

            if (t.path != newPath)
            {
                string err = AssetDatabase.MoveAsset(t.path, newPath);
                if (!string.IsNullOrEmpty(err))
                {
                    Debug.LogError($"[EventEditor] Rename 失敗: {t.path} -> {newPath}\n{err}");
                }
            }
            branch++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EventEditor] 枝番振り直し完了（章{chapter}-節{section}）。");
    }

    // ───────── ヘルパ ─────────
    private static int FindNextBranchNumber(string savePath, int chapter, int section)
    {
        int max = 0;
        var guids = AssetDatabase.FindAssets("t:EventData", new[] { savePath });
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var name = Path.GetFileNameWithoutExtension(p);
            var m = NameRegex.Match(name);
            if (!m.Success) continue;
            int ch = int.Parse(m.Groups[1].Value);
            int se = int.Parse(m.Groups[2].Value);
            if (ch != chapter || se != section) continue;

            int br = int.Parse(m.Groups[3].Value);
            if (br > max) max = br;
        }
        return max + 1; // 次の枝番
    }

    private static string SanitizeFileName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "NewEvent";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }
}
#endif
