#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace AnoGame.SLFBDebug
{
    public class SavedataSwitcherWindow : EditorWindow
    {
        private string folderPath;
        private string filePattern = "savedata_*.json"; // ここを変えれば任意パターンにも対応
        private List<FileInfo> files = new List<FileInfo>();
        private int selectedIndex = -1;
        private Vector2 scroll;
        private bool makeBackup = true;
        private string newSaveDataName = "";

        public static void ShowWindow()
        {
            var w = GetWindow<SavedataSwitcherWindow>("Savedata Switcher");
            w.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(folderPath))
                folderPath = UnityEngine.Application.persistentDataPath; // 既定は persistentDataPath
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("セーブフォルダ", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            folderPath = EditorGUILayout.TextField(folderPath);
            if (GUILayout.Button("選択", GUILayout.Width(64)))
            {
                var p = EditorUtility.OpenFolderPanel("セーブフォルダを選択", folderPath, "");
                if (!string.IsNullOrEmpty(p))
                {
                    folderPath = p;
                    Refresh();
                }
            }
            if (GUILayout.Button("persistentDataPath", GUILayout.Width(160)))
            {
                folderPath = UnityEngine.Application.persistentDataPath;
                Refresh();
            }
            if (GUILayout.Button("開く", GUILayout.Width(64)))
            {
                if (Directory.Exists(folderPath)) EditorUtility.RevealInFinder(folderPath);
            }
            if (GUILayout.Button("再読み込み", GUILayout.Width(90)))
            {
                Refresh();
            }
            EditorGUILayout.EndHorizontal();

            filePattern = EditorGUILayout.TextField(
                new GUIContent("検索パターン", "Directory.GetFiles のワイルドカード(*, ?)が使えます"),
                filePattern);

            var dest = Path.Combine(folderPath ?? "", "savedata.json");
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"出力先: {dest}");
                if (File.Exists(dest))
                {
                    var fi = new FileInfo(dest);
                    EditorGUILayout.LabelField($"現在の savedata.json: {fi.Length} bytes, 更新 {fi.LastWriteTime:yyyy/MM/dd HH:mm:ss}");
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                newSaveDataName = EditorGUILayout.TextField("作成名: savedata_", newSaveDataName);
                GUILayout.Label(".json");
                if (GUILayout.Button("作成", GUILayout.Width(60)))
                {
                    CreateNewSaveData();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("候補ファイル（新しい順）", EditorStyles.boldLabel);

            using (var sv = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = sv.scrollPosition;

                for (int i = 0; i < files.Count; i++)
                {
                    var fi = files[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // 選択トグル
                    bool isSelected = (i == selectedIndex);
                    if (GUILayout.Toggle(isSelected, GUIContent.none, GUILayout.Width(18)) != isSelected)
                        selectedIndex = i;

                    GUILayout.Label(fi.Name, GUILayout.MinWidth(180));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{fi.Length / 1024f:0.0} KB", GUILayout.Width(80));
                    GUILayout.Label(fi.LastWriteTime.ToString("yyyy/MM/dd HH:mm"), GUILayout.Width(140));

                    if (GUILayout.Button("表示", GUILayout.Width(50)))
                        EditorUtility.RevealInFinder(fi.FullName);

                    if (GUILayout.Button("コピー", GUILayout.Width(60)))
                    {
                        selectedIndex = i;
                        CopySelectedToDest();
                    }

                    if (GUILayout.Button("削除", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("削除確認", $"{fi.Name} を削除しますか？\nこの操作は取り消せません。", "削除", "キャンセル"))
                        {
                            DeleteFile(fi.FullName);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (files.Count == 0)
                {
                    EditorGUILayout.HelpBox("一致するファイルが見つかりません。フォルダ/パターンを確認してください。", MessageType.Info);
                }
            }

            EditorGUILayout.Space(8);
            makeBackup = EditorGUILayout.ToggleLeft("既存の savedata.json をタイムスタンプ付きでバックアップする", makeBackup);

            using (new EditorGUI.DisabledScope(selectedIndex < 0 || files.Count == 0))
            {
                if (GUILayout.Button("選択中を savedata.json にコピー"))
                {
                    CopySelectedToDest();
                }
            }
        }

        private void Refresh()
        {
            files.Clear();
            selectedIndex = -1;

            try
            {
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    var di = new DirectoryInfo(folderPath);
                    files = di.GetFiles(filePattern, SearchOption.TopDirectoryOnly)
                            .Where(f => !string.Equals(f.Name, "savedata.json", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(f => f.LastWriteTime)
                            .ToList();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification(new GUIContent("ファイル一覧の取得に失敗しました"));
            }
            Repaint();
        }

        private void CopySelectedToDest()
        {
            if (selectedIndex < 0 || selectedIndex >= files.Count) return;

            var src = files[selectedIndex].FullName;
            var dest = Path.Combine(folderPath, "savedata.json");

            try
            {
                if (makeBackup && File.Exists(dest))
                {
                    var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var backup = Path.Combine(folderPath, $"savedata.json.bak.{ts}");
                    File.Copy(dest, backup, overwrite: false);
                }

                File.Copy(src, dest, overwrite: true);
                AssetDatabase.Refresh(); // 何かしらエディタに反映したい時に
                ShowNotification(new GUIContent($"コピー完了: {Path.GetFileName(src)} → savedata.json"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("コピー失敗", $"エラー: {e.Message}", "OK");
            }
        }

        private void CreateNewSaveData()
        {
            if (string.IsNullOrEmpty(newSaveDataName))
            {
                EditorUtility.DisplayDialog("エラー", "作成名を入力してください", "OK");
                return;
            }

            var dest = Path.Combine(folderPath ?? "", "savedata.json");
            if (!File.Exists(dest))
            {
                EditorUtility.DisplayDialog("エラー", "コピー元の savedata.json が見つかりません", "OK");
                return;
            }

            var newFileName = $"savedata_{newSaveDataName}.json";
            var newFilePath = Path.Combine(folderPath, newFileName);

            if (File.Exists(newFilePath))
            {
                if (!EditorUtility.DisplayDialog("確認", $"{newFileName} は既に存在します。\n上書きしますか？", "はい", "いいえ"))
                {
                    return;
                }
            }

            try
            {
                File.Copy(dest, newFilePath, overwrite: true);
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent($"作成完了: {newFileName}"));
                newSaveDataName = ""; // Reset input
                Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("作成失敗", $"エラー: {e.Message}", "OK");
            }
        }

        private void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    AssetDatabase.Refresh();
                    ShowNotification(new GUIContent("削除しました"));
                    Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("削除失敗", $"エラー: {e.Message}", "OK");
            }
        }
    }

}
#endif
