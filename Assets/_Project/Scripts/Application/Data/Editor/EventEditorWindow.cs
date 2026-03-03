#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using AnoGame.Data;

public sealed class EventEditorWindow : EditorWindow
{
    private struct LabelWidthScope : IDisposable
    {
        private readonly float _old;
        public LabelWidthScope(float width)
        {
            _old = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
        }
        public void Dispose() => EditorGUIUtility.labelWidth = _old;
    }
    // ───────── 設定 ─────────
    private const string DEFAULT_SAVE_PATH = "Assets/_Project/Data/Items";
    private const string EDITORPREFS_PATH_KEY = "AnoGame.EventEditor.SavePath";
    private static readonly Regex NameRegex =
        new Regex(@"^(\d+)-(\d+)\.(\d{3})\.(.+)$", RegexOptions.Compiled);

    // ───────── 作成用 GUI 状態 ─────────
    private EventData _draft;
    private SerializedObject _so;
    private SerializedProperty _spEventId, _spEventName, _spDescription, _spIsOneTime;

    private static readonly int[] ChapterOptions = Enumerable.Range(1, 5).ToArray();
    private static readonly int[] SectionOptions = Enumerable.Range(1, 5).ToArray();
    private int _chapterIndex, _sectionIndex;
    private string _savePath;

    // ───────── 並べ替え用 GUI 状態 ─────────
    private enum Tab { Create, Reorder }
    private Tab _tab;

    private class Entry
    {
        public string guid;
        public string path;     // Assets/..../1-2.003.名前.asset
        public string namePart; // 名前（拡張子なしから枝番を除いた部分）
        public int chapter, section, branch;
        public override string ToString() => $"{chapter}-{section}.{branch:000}.{namePart}";
    }
    private readonly List<Entry> _entries = new();
    private ReorderableList _list;
    private Vector2 _listScroll;

    // [MenuItem("Tools/EventEditor")]
    public static void ShowWindow()
    {
        var w = GetWindow<EventEditorWindow>("Event Editor");
        w.minSize = new Vector2(520, 460);
        w.Show();
    }

    private void OnEnable()
    {
        EnsureSerializedObject();

        _savePath = EditorPrefs.GetString(EDITORPREFS_PATH_KEY, DEFAULT_SAVE_PATH);
        _chapterIndex = 0; _sectionIndex = 0;

        BuildReorderableList();
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(_savePath))
            EditorPrefs.SetString(EDITORPREFS_PATH_KEY, _savePath);

        if (_draft != null)
        {
            DestroyImmediate(_draft);
            _draft = null;
        }

        _so = null;
        _spEventId = _spEventName = _spDescription = _spIsOneTime = null;
    }


    private void OnGUI()
    {
        // タブ
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Toggle(_tab == Tab.Create, "作成", EditorStyles.toolbarButton)) _tab = Tab.Create;
            if (GUILayout.Toggle(_tab == Tab.Reorder, "並べ替え", EditorStyles.toolbarButton)) _tab = Tab.Reorder;
        }
        EditorGUILayout.Space(4);

        // 章・節（両タブ共通）
        DrawChapterSectionPicker();

        if (_tab == Tab.Create) DrawCreateTab();
        else DrawReorderTab();
    }

    private void EnsureSerializedObject()
    {
        // target が死んでいたら作り直す
        if (_draft == null)
        {
            _draft = ScriptableObject.CreateInstance<EventData>();
        }

        if (_so == null || _so.targetObject == null)
        {
            _so = new SerializedObject(_draft);
            _spEventId = _so.FindProperty("eventId");
            _spEventName = _so.FindProperty("eventName");
            _spDescription = _so.FindProperty("description");
            _spIsOneTime = _so.FindProperty("isOneTime");
        }
    }

    // ───────── タブ：作成 ─────────
    private void DrawCreateTab()
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EnsureSerializedObject();
            if (_so == null || _so.targetObject == null) return;

            GUILayout.Label("EventData プロパティ", EditorStyles.boldLabel);

            _so.Update();
            EditorGUILayout.PropertyField(_spEventId, new GUIContent("Event Id"));
            EditorGUILayout.PropertyField(_spEventName, new GUIContent("Event Name"));
            EditorGUILayout.PropertyField(_spDescription, new GUIContent("Description"), GUILayout.MinHeight(48));
            EditorGUILayout.PropertyField(_spIsOneTime, new GUIContent("Is One Time"));
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
                        else EditorUtility.DisplayDialog("無効なパス", "Assets 配下を選択してください。", "OK");
                    }
                }
                if (GUILayout.Button("既定に戻す", GUILayout.Width(100)))
                    _savePath = DEFAULT_SAVE_PATH;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存（作成）", GUILayout.Height(26)))
                    SaveEvent();

                if (GUILayout.Button("（現在の章‐節）を自動採番で振り直し", GUILayout.Height(26)))
                {
                    AutoRenumberForCurrentChapterSection();
                    // Reorder タブにも反映
                    ReloadEntries();
                }
            }
        }
    }

    // ───────── タブ：並べ替え ─────────
    private void DrawReorderTab()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label($"並べ替え（{CurrentChapter}-{CurrentSection}）", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("リストを読み込み/更新", GUILayout.Width(180)))
                    ReloadEntries();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("この順番で枝番を振り直す", GUILayout.Height(24)))
                    RenumberByCurrentListOrder();
            }

            EditorGUILayout.Space(6);

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox(
                "ドラッグ＆ドロップで順序を入れ替え → ボタンで 001,002… と連番を振り直します。\n" +
                "リネームは AssetDatabase.MoveAsset を用いるため .meta の GUID は維持され、参照は切れません。",
                MessageType.Info);
        }
    }

    // ───────── 共通 UI ─────────
    // 置き換え
    private void DrawChapterSectionPicker()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("章 / 節", EditorStyles.boldLabel);

            // 1行目：章・節（左寄せのみ）
            using (new EditorGUILayout.HorizontalScope())
            using (new LabelWidthScope(24f)) // 「章」「節」のラベル幅を小さく固定
            {
                // 幅も固定してコンパクトに
                _chapterIndex = EditorGUILayout.Popup(
                    new GUIContent("章"),
                    _chapterIndex,
                    ChapterOptions.Select(i => i.ToString()).ToArray(),
                    GUILayout.Width(70));   // 例：ラベル＋ドロップダウンで約140px

                GUILayout.Space(8);

                _sectionIndex = EditorGUILayout.Popup(
                    new GUIContent("節"),
                    _sectionIndex,
                    SectionOptions.Select(i => i.ToString()).ToArray(),
                    GUILayout.Width(70));

                // ここでは FlexibleSpace を置かない → 左側に寄る
            }

            // 2行目：保存先の表示（幅を取るので別行に）
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("保存先:", GUILayout.Width(50));
                EditorGUILayout.SelectableLabel(_savePath, GUILayout.Height(16));
            }
        }
    }

    // ───────── 並べ替え内部 ─────────
    private void BuildReorderableList()
    {
        _list = new ReorderableList(_entries, typeof(Entry), true, true, false, false);
        _list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "イベント一覧（ドラッグで順序変更）");
        };
        _list.drawElementCallback = (rect, index, active, focused) =>
        {
            if (index < 0 || index >= _entries.Count) return;
            var e = _entries[index];
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(rect, $"{index + 1,2}. {e.chapter}-{e.section}.{e.branch:000}.{e.namePart}");
        };
        _list.onSelectCallback = l =>
        {
            if (l.index >= 0 && l.index < _entries.Count)
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_entries[l.index].path));
        };
    }

    private void ReloadEntries()
    {
        _entries.Clear();
        if (!AssetDatabase.IsValidFolder(_savePath))
        {
            AssetDatabase.Refresh();
            return;
        }

        int chapter = CurrentChapter;
        int section = CurrentSection;

        var guids = AssetDatabase.FindAssets("t:EventData", new[] { _savePath });
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var name = Path.GetFileNameWithoutExtension(p);
            var m = NameRegex.Match(name);
            if (!m.Success) continue;
            int ch = int.Parse(m.Groups[1].Value);
            int se = int.Parse(m.Groups[2].Value);
            if (ch != chapter || se != section) continue;

            var entry = new Entry
            {
                guid = g,
                path = p,
                chapter = ch,
                section = se,
                branch = int.Parse(m.Groups[3].Value),
                namePart = m.Groups[4].Value
            };
            _entries.Add(entry);
        }

        // 現在の枝番順で表示開始（ここから自由に入れ替え）
        _entries.Sort((a, b) => a.branch.CompareTo(b.branch));
        _list.index = -1;
        Repaint();
    }

    private void RenumberByCurrentListOrder()
    {
        if (_entries.Count == 0)
        {
            EditorUtility.DisplayDialog("情報", "並べ替える対象がありません。まずリストを読み込んでください。", "OK");
            return;
        }

        int chapter = CurrentChapter;
        int section = CurrentSection;

        // 現在の並び順に 001,002… を付けてリネーム
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            string dir = Path.GetDirectoryName(e.path).Replace("\\", "/");
            string newName = $"{chapter}-{section}.{(i + 1):000}.{e.namePart}";
            string newPath = $"{dir}/{newName}.asset";

            if (e.path != newPath)
            {
                string err = AssetDatabase.MoveAsset(e.path, newPath);
                if (!string.IsNullOrEmpty(err))
                {
                    Debug.LogError($"[EventEditor] Rename 失敗: {e.path} -> {newPath}\n{err}");
                }
                else
                {
                    e.path = newPath;
                    e.branch = i + 1;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EventEditor] 並べ替え順で枝番振り直し完了（章{chapter}-節{section}）。");
        // 再読み込みして確定順を表示
        ReloadEntries();
    }

    private void AutoRenumberForCurrentChapterSection()
    {
        // 名前昇順で自動並べ（従来の一括振り直し）
        ReloadEntries();
        _entries.Sort((a, b) => string.Compare(a.namePart, b.namePart, StringComparison.CurrentCulture));
        RenumberByCurrentListOrder();
    }

    private int CurrentChapter => ChapterOptions[_chapterIndex];
    private int CurrentSection => SectionOptions[_sectionIndex];

    // ───────── 保存（作成）─────────
    private void SaveEvent()
    {
        string evName = (_spEventName.stringValue ?? "").Trim();
        if (string.IsNullOrEmpty(evName))
        {
            EditorUtility.DisplayDialog("エラー", "Event Name を入力してください。", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(_savePath))
        {
            Directory.CreateDirectory(_savePath);
            AssetDatabase.Refresh();
        }

        int chapter = CurrentChapter;
        int section = CurrentSection;
        int next = FindNextBranchNumber(_savePath, chapter, section);

        string safeName = SanitizeFileName(evName);
        string fileNameNoExt = $"{chapter}-{section}.{next:000}.{safeName}";
        string path = $"{_savePath}/{fileNameNoExt}.asset";

        var newAsset = ScriptableObject.CreateInstance<EventData>();
        EditorUtility.CopySerialized(_draft, newAsset);
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(newAsset);

        Debug.Log($"[EventEditor] 生成: {path}");
        // リストにも反映
        ReloadEntries();
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
        return max + 1;
    }

    private static string SanitizeFileName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "NewEvent";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
#endif
