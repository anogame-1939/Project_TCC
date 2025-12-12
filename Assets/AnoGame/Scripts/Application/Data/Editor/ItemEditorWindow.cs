#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

using AnoGame.Data;

public sealed class ItemEditorWindow : EditorWindow
{
    // ───────────── 設定（要件：デフォルトパスは const） ─────────────
    private const string DEFAULT_SAVE_PATH = "Assets/AnoGame/Data/Items";
    private const string EDITORPREFS_PATH_KEY = "AnoGame.ItemEditor.SavePath";

    // 命名規則：Item Data.{chapter}-{section}.{branch:000}.{itemName}.asset
    private const string FILE_PREFIX = "Item Data";   // 画面の見た目と合わせてスペース入り
    private static readonly Regex NameRegex =
        new Regex(@"^Item Data\.(\d+)-(\d+)\.(\d{3})\.(.+)$", RegexOptions.Compiled);

    // ───────────── GUI 状態 ─────────────
    // 作業用ドラフト（SerializedObjectで公式ドローアを使って描画）
    private ItemData _draft;
    private SerializedObject _so;
    private SerializedProperty _spItemId;
    private SerializedProperty _spItemName;
    private SerializedProperty _spDescription;
    private SerializedProperty _spAssetReference;
    private SerializedProperty _spItemType;
    private SerializedProperty _spIsStackable;
    private SerializedProperty _spMaxStackSize;

    // 章・節
    private static readonly int[] ChapterOptions = Enumerable.Range(1, 30).ToArray();
    private static readonly int[] SectionOptions = Enumerable.Range(1, 30).ToArray();
    private int _chapterIndex;
    private int _sectionIndex;

    // 保存先・DB
    private string _savePath;
    private ItemDatabase _itemDatabase;

    // ───────────── メニュー ─────────────
    // [MenuItem("Tools/ItemEditor")]
    public static void ShowWindow()
    {
        var window = GetWindow<ItemEditorWindow>("Item Editor");
        window.minSize = new Vector2(480, 420);
        window.Show();
    }

    private void OnEnable()
    {
        if (_draft == null)
        {
            _draft = ScriptableObject.CreateInstance<ItemData>();
        }
        _so = new SerializedObject(_draft);
        _spItemId = _so.FindProperty("itemId");
        _spItemName = _so.FindProperty("itemName");
        _spDescription = _so.FindProperty("description");
        _spAssetReference = _so.FindProperty("assetReference");
        _spItemType = _so.FindProperty("itemType");
        _spIsStackable = _so.FindProperty("isStackable");
        _spMaxStackSize = _so.FindProperty("maxStackSize");

        _savePath = EditorPrefs.GetString(EDITORPREFS_PATH_KEY, DEFAULT_SAVE_PATH);
        // 初期は 1-1
        _chapterIndex = 0;
        _sectionIndex = 0;
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("章 / 節", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _chapterIndex = EditorGUILayout.Popup(new GUIContent("章"), _chapterIndex, ChapterOptions.Select(i => i.ToString()).ToArray(), GUILayout.MaxWidth(250));
                _sectionIndex = EditorGUILayout.Popup(new GUIContent("節"), _sectionIndex, SectionOptions.Select(i => i.ToString()).ToArray(), GUILayout.MaxWidth(250));
            }
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("ItemData プロパティ", EditorStyles.boldLabel);
            _so.Update();
            EditorGUILayout.PropertyField(_spItemId, new GUIContent("Item Id"));
            EditorGUILayout.PropertyField(_spItemName, new GUIContent("Item Name"));
            EditorGUILayout.PropertyField(_spDescription, new GUIContent("Description"), GUILayout.MinHeight(48));
            EditorGUILayout.PropertyField(_spAssetReference, new GUIContent("Asset Reference"));
            EditorGUILayout.PropertyField(_spItemType, new GUIContent("Item Type"));
            EditorGUILayout.PropertyField(_spIsStackable, new GUIContent("Is Stackable"));
            EditorGUILayout.PropertyField(_spMaxStackSize, new GUIContent("Max Stack Size"));
            _so.ApplyModifiedProperties();
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("保存設定", EditorStyles.boldLabel);
            _itemDatabase = (ItemDatabase)EditorGUILayout.ObjectField(new GUIContent("Item Database"), _itemDatabase, typeof(ItemDatabase), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("保存先 (相対)", GUILayout.Width(120));
                _savePath = EditorGUILayout.TextField(_savePath);
                if (GUILayout.Button("選択...", GUILayout.Width(80)))
                {
                    var selected = EditorUtility.OpenFolderPanel("保存先フォルダを選択", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        // プロジェクト直下の Assets 相対に直す
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

            if (GUILayout.Button("保存（作成してDBへ追加）", GUILayout.Height(30)))
            {
                SaveItem();
            }
        }
    }

    // ───────────── 保存処理 ─────────────
    private void SaveItem()
    {
        if (string.IsNullOrWhiteSpace(_spItemName.stringValue))
        {
            EditorUtility.DisplayDialog("エラー", "Item Name を入力してください。", "OK");
            return;
        }
        if (_itemDatabase == null)
        {
            EditorUtility.DisplayDialog("エラー", "ItemDatabase を割り当ててください。", "OK");
            return;
        }

        // 保存先の用意
        if (!AssetDatabase.IsValidFolder(_savePath))
        {
            Directory.CreateDirectory(_savePath);
            AssetDatabase.Refresh();
        }

        // 章・節・枝番の決定
        int chapter = ChapterOptions[_chapterIndex];
        int section = SectionOptions[_sectionIndex];
        string itemName = SanitizeFileName(_spItemName.stringValue);

        // 現在のDBから「同一 章-節 + 同一アイテム名」の枝番を採番
        int nextBranch = GetNextBranchNumber(_itemDatabase, chapter, section, itemName);

        string fileNameCore = $"{FILE_PREFIX}.{chapter}-{section}.{nextBranch:000}.{itemName}";
        string assetPath = $"{_savePath}/{fileNameCore}.asset";

        // 同名ファイルが存在したらユニーク化（極稀な競合対策）
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        // 新規 SO を作成し、ドラフト内容をコピー
        var newItem = ScriptableObject.CreateInstance<ItemData>();
        EditorUtility.CopySerialized(_draft, newItem);
        AssetDatabase.CreateAsset(newItem, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // DB に追加（List<ItemData> へのアクセスは SerializedObject 経由で安全に行う）
        var dbSO = new SerializedObject(_itemDatabase);
        var itemsProp = dbSO.FindProperty("items");
        // 同名の ItemName が既にDBにある場合は警告
        bool duplicatedName = false;
        for (int i = 0; i < itemsProp.arraySize; i++)
        {
            var elem = itemsProp.GetArrayElementAtIndex(i).objectReferenceValue as ItemData;
            if (elem != null && elem.ItemName == newItem.ItemName)
            {
                duplicatedName = true;
                break;
            }
        }
        itemsProp.arraySize++;
        itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1).objectReferenceValue = newItem;
        dbSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(_itemDatabase);

        if (duplicatedName)
        {
            Debug.LogWarning($"[ItemEditor] 同じ ItemName が既に ItemDatabase に存在します: \"{newItem.ItemName}\"");
        }

        EditorGUIUtility.PingObject(newItem);
        Debug.Log($"[ItemEditor] 生成: {assetPath} 章節={chapter}-{section} 枝番={nextBranch:000}");

        // 作り終えたらドラフトは名前以外そのまま継続利用（連続作成しやすい）
    }

    private static string SanitizeFileName(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
        {
            s = s.Replace(c, '_');
        }
        return s.Trim();
    }

    private static int GetNextBranchNumber(ItemDatabase db, int chapter, int section, string itemName)
    {
        var used = new List<int>();

        // DB内の各 Item のアセットパスから命名規則を解析
        foreach (var item in db.Items)
        {
            if (item == null) continue;
            var path = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrEmpty(path)) continue;

            // 拡張子抜きのファイル名
            var file = Path.GetFileNameWithoutExtension(path);
            var m = NameRegex.Match(file);
            if (!m.Success) continue;

            int ch = int.Parse(m.Groups[1].Value);
            int se = int.Parse(m.Groups[2].Value);
            int br = int.Parse(m.Groups[3].Value);
            string nm = m.Groups[4].Value;

            if (ch == chapter && se == section && string.Equals(nm, itemName, StringComparison.Ordinal))
            {
                used.Add(br);
            }
        }

        if (used.Count == 0) return 1;                 // 初回は 001 から
        return used.Max() + 1;                          // 既存最大の次
    }

    private void OnDisable()
    {
        // 保存先の永続化
        if (!string.IsNullOrEmpty(_savePath))
        {
            EditorPrefs.SetString(EDITORPREFS_PATH_KEY, _savePath);
        }
        // 作業用ドラフトはメモリ SO なので破棄
        if (_draft != null)
        {
            DestroyImmediate(_draft);
        }
    }
}
#endif
