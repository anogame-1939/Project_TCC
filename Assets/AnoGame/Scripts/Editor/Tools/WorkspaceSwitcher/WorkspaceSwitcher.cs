#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// Workspace Switcher - フローティングパネル
    /// 複数タブを持つ独立ウィンドウグループのみをボタン表示し、
    /// クリックでそのグループの先頭ウィンドウを前面表示する。
    /// ダブルクリックでインライン名前変更。
    /// Win32 APIで真の常時前面表示。
    /// </summary>
    public class WorkspaceSwitcher : EditorWindow
    {
        private const string CustomNamesPrefsKey = "AnoGame_WorkspaceSwitcher_CustomNames";
        private const double RefreshInterval = 0.5;

        private double _lastRefreshTime;
        private List<WindowGroup> _groups = new List<WindowGroup>();
        private Dictionary<string, string> _customNames = new Dictionary<string, string>();

        // インラインリネーム用
        private string _renamingKey;       // 現在リネーム中のグループKey (null=リネームなし)
        private string _renamingText;      // 編集中のテキスト
        private bool _renameFocusNeeded;   // フォーカス要求フラグ

        private class WindowGroup
        {
            public string Key;
            public string DefaultLabel;
            public string Tooltip;
            public EditorWindow FirstWindow;
            public int TabCount;
        }

        // Reflection cache
        private static Type _dockAreaType;
        private static FieldInfo _panesField;
        private static PropertyInfo _viewWindowProperty;
        private static FieldInfo _mParentField;
        private static bool _reflectionReady;

        // Win32 API for true TopMost
#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private bool _topMostApplied;
#endif

        [MenuItem("AnoGame/Tools/Workspace Switcher")]
        public static void ShowWindow()
        {
            var existing = Resources.FindObjectsOfTypeAll<WorkspaceSwitcher>().FirstOrDefault();
            if (existing != null)
            {
                existing.Focus();
                return;
            }

            var wnd = CreateInstance<WorkspaceSwitcher>();
            wnd.titleContent = new GUIContent("Workspace");
            wnd.ShowUtility();
            wnd.PositionTopRight();
        }

        private void OnEnable()
        {
            InitReflection();
            LoadCustomNames();
            RefreshGroups();
        }

        private void PositionTopRight()
        {
            var mainWnd = EditorGUIUtility.GetMainWindowPosition();
            float w = 220;
            float h = 160;
            float margin = 10;
            position = new Rect(
                mainWnd.xMax - w - margin,
                mainWnd.y + margin,
                w, h
            );
        }

        private void ApplyTopMost()
        {
#if UNITY_EDITOR_WIN
            if (_topMostApplied) return;

            EditorApplication.delayCall += () =>
            {
                var hwnd = FindWindow(null, "Workspace");
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    _topMostApplied = true;
                }
            };
#endif
        }

        private void OnGUI()
        {
            // TopMost適用
            ApplyTopMost();

            if (EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                RefreshGroups();
            }

            // 背景
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height),
                new Color(0.12f, 0.12f, 0.12f, 0.95f));

            GUILayout.Space(4);

            if (_groups.Count == 0)
            {
                GUILayout.Label("No multi-tab windows", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // クリックアウトでリネーム確定検出
            if (_renamingKey != null
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0)
            {
                // ボタン領域外のクリックならリネーム確定
                CommitRename();
            }

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                string displayName = GetDisplayName(g);
                bool isRenaming = (_renamingKey == g.Key);

                if (isRenaming)
                {
                    DrawRenameField(g);
                }
                else
                {
                    DrawGroupButton(g, displayName);
                }
            }
        }

        // ─────────────────────────────────────────────
        // Button / Inline Rename
        // ─────────────────────────────────────────────

        private void DrawGroupButton(WindowGroup g, string displayName)
        {
            var content = new GUIContent($" {displayName}  ({g.TabCount})", g.Tooltip);

            var style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 26,
                margin = new RectOffset(4, 4, 1, 1),
                padding = new RectOffset(8, 8, 4, 4)
            };

            var btnRect = GUILayoutUtility.GetRect(content, style);

            if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 2)
                {
                    // ダブルクリック: インラインリネーム開始
                    Event.current.Use();
                    _renamingKey = g.Key;
                    _renamingText = displayName;
                    _renameFocusNeeded = true;
                    Repaint();
                    return;
                }
                else if (Event.current.clickCount == 1 && Event.current.button == 0)
                {
                    // シングルクリック: ウィンドウ前面表示
                    Event.current.Use();
                    if (g.FirstWindow != null)
                    {
                        g.FirstWindow.Focus();
                        // TopMost再適用（Focusで他のウィンドウが前に来るため）
#if UNITY_EDITOR_WIN
                        _topMostApplied = false;
#endif
                    }
                    return;
                }
            }

            GUI.Button(btnRect, content, style);
        }

        private void DrawRenameField(WindowGroup g)
        {
            var fieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 26,
                margin = new RectOffset(4, 4, 1, 1),
                padding = new RectOffset(8, 8, 4, 4)
            };

            GUI.SetNextControlName("InlineRename");
            _renamingText = GUILayout.TextField(_renamingText, fieldStyle);

            if (_renameFocusNeeded)
            {
                EditorGUI.FocusTextInControl("InlineRename");
                _renameFocusNeeded = false;
            }

            // Enter で確定
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                Event.current.Use();
                CommitRename();
            }
            // Escape でキャンセル
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                _renamingKey = null;
                Repaint();
            }
        }

        private void CommitRename()
        {
            if (_renamingKey == null) return;

            if (!string.IsNullOrEmpty(_renamingText))
            {
                _customNames[_renamingKey] = _renamingText;
                SaveCustomNames();
            }
            _renamingKey = null;
            Repaint();
        }

        // ─────────────────────────────────────────────
        // Display Name
        // ─────────────────────────────────────────────

        private string GetDisplayName(WindowGroup g)
        {
            if (_customNames.TryGetValue(g.Key, out string custom) && !string.IsNullOrEmpty(custom))
            {
                return custom;
            }
            return g.DefaultLabel;
        }

        // ─────────────────────────────────────────────
        // Data Refresh
        // ─────────────────────────────────────────────

        private void RefreshGroups()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            _groups.Clear();

            if (_mParentField == null || _dockAreaType == null || _panesField == null) return;

            var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var containerTabs = new Dictionary<object, List<EditorWindow>>();

            foreach (var w in allWindows)
            {
                if (w == null || w == this) continue;

                var dockArea = _mParentField.GetValue(w);
                if (dockArea == null || !_dockAreaType.IsInstanceOfType(dockArea)) continue;

                object containerWindow = null;
                if (_viewWindowProperty != null)
                {
                    containerWindow = _viewWindowProperty.GetValue(dockArea);
                }
                if (containerWindow == null) continue;

                if (!containerTabs.TryGetValue(containerWindow, out var list))
                {
                    list = new List<EditorWindow>();
                    containerTabs[containerWindow] = list;
                }
                list.Add(w);
            }

            // メインウィンドウ（タブ数最大）を除外
            object mainContainer = null;
            int maxCount = 0;
            foreach (var kvp in containerTabs)
            {
                if (kvp.Value.Count > maxCount)
                {
                    maxCount = kvp.Value.Count;
                    mainContainer = kvp.Key;
                }
            }

            foreach (var kvp in containerTabs)
            {
                if (kvp.Key == mainContainer) continue;
                var windows = kvp.Value;
                if (windows.Count < 2) continue;

                string key = string.Join("|",
                    windows.Select(w => w.GetType().FullName).OrderBy(n => n));

                var first = windows[0];
                string tooltip = string.Join("\n",
                    windows.Select(w => $"{w.titleContent.text} ({w.GetType().Name})"));

                _groups.Add(new WindowGroup
                {
                    Key = key,
                    DefaultLabel = first.titleContent.text,
                    Tooltip = tooltip,
                    FirstWindow = first,
                    TabCount = windows.Count
                });
            }

            _groups = _groups.OrderBy(g => GetDisplayName(g)).ToList();
            AdjustWindowSize();
            Repaint();
        }

        private void AdjustWindowSize()
        {
            float btnHeight = 28;
            float padding = 12;
            float minHeight = 40;
            float newHeight = Mathf.Max(minHeight, _groups.Count * btnHeight + padding);

            var pos = position;
            pos.height = newHeight;
            position = pos;
        }

        // ─────────────────────────────────────────────
        // Persistence
        // ─────────────────────────────────────────────

        private void LoadCustomNames()
        {
            _customNames.Clear();
            string json = EditorPrefs.GetString(CustomNamesPrefsKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<NameStore>(json);
                if (data != null)
                {
                    for (int i = 0; i < data.Keys.Count; i++)
                    {
                        _customNames[data.Keys[i]] = data.Values[i];
                    }
                }
            }
            catch { }
        }

        private void SaveCustomNames()
        {
            var data = new NameStore();
            foreach (var kvp in _customNames)
            {
                data.Keys.Add(kvp.Key);
                data.Values.Add(kvp.Value);
            }
            EditorPrefs.SetString(CustomNamesPrefsKey, JsonUtility.ToJson(data));
        }

        [Serializable]
        private class NameStore
        {
            public List<string> Keys = new List<string>();
            public List<string> Values = new List<string>();
        }

        // ─────────────────────────────────────────────
        // Reflection
        // ─────────────────────────────────────────────

        private static void InitReflection()
        {
            if (_reflectionReady) return;
            _reflectionReady = true;

            var asm = typeof(UnityEditor.Editor).Assembly;
            _dockAreaType = asm.GetType("UnityEditor.DockArea");

            if (_dockAreaType != null)
            {
                _panesField = _dockAreaType.GetField("m_Panes",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }

            var viewType = asm.GetType("UnityEditor.View");
            if (viewType != null)
            {
                _viewWindowProperty = viewType.GetProperty("window",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            _mParentField = typeof(EditorWindow).GetField("m_Parent",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
#endif
