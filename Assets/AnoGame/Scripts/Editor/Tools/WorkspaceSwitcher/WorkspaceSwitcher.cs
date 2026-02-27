#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// Workspace Switcher - フローティングパネル
    /// 複数タブを持つ独立ウィンドウグループをラベル形式で表示。
    /// クリックで先頭ウィンドウFocus、ダブルクリックでインラインリネーム、ドラッグで並び替え。
    /// </summary>
    public class WorkspaceSwitcher : EditorWindow
    {
        private const string CustomNamesPrefsKey = "AnoGame_WorkspaceSwitcher_CustomNames";
        private const string OrderPrefsKey = "AnoGame_WorkspaceSwitcher_Order";

        private List<WindowGroupData> _groups = new List<WindowGroupData>();
        private Dictionary<string, string> _customNames = new Dictionary<string, string>();
        private List<string> _order = new List<string>(); // Key順序保持

        private VisualElement _listContainer;
        private IVisualElementScheduledItem _refreshSchedule;

        // Drag state
        private bool _isDragging;
        private bool _dragStarted;
        private Vector2 _dragStartPos;
        private int _dragIndex;
        private VisualElement _dragGhost;
        private VisualElement _dropIndicator;
        private const float DragThreshold = 5f;
        private IVisualElementScheduledItem _pendingClickSchedule;

        private class WindowGroupData
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

        // Win32 TopMost
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
            LoadOrder();
        }

        private void PositionTopRight()
        {
            var mainWnd = EditorGUIUtility.GetMainWindowPosition();
            float w = 200;
            float h = 120;
            float margin = 10;
            position = new Rect(mainWnd.xMax - w - margin, mainWnd.y + margin, w, h);
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

        // ─────────────────────────────────────────────
        // UIToolkit
        // ─────────────────────────────────────────────

        public void CreateGUI()
        {
            var root = rootVisualElement;

            // Global style
            var style = new StyleSheet();
            root.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.97f);
            root.style.paddingTop = 2;
            root.style.paddingBottom = 2;
            root.style.paddingLeft = 0;
            root.style.paddingRight = 0;

            _listContainer = new VisualElement();
            root.Add(_listContainer);

            RefreshGroups();

            // 定期更新
            _refreshSchedule = root.schedule.Execute(RefreshGroups).Every(500);

            ApplyTopMost();
        }

        // ─────────────────────────────────────────────
        // Refresh & Build
        // ─────────────────────────────────────────────

        private void RefreshGroups()
        {
            var oldGroups = _groups.Select(g => g.Key).ToList();
            BuildGroupData();

            // グループが変わった場合のみUI再構築
            var newKeys = _groups.Select(g => g.Key).ToList();
            if (!oldGroups.SequenceEqual(newKeys))
            {
                RebuildList();
            }

            AdjustWindowSize();
        }

        private void BuildGroupData()
        {
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
                    containerWindow = _viewWindowProperty.GetValue(dockArea);
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
                if (windows.Count < 1) continue;

                string key = string.Join("|",
                    windows.Select(w => w.GetType().FullName).OrderBy(n => n));

                var first = windows[0];
                string tooltip = string.Join("\n",
                    windows.Select(w => $"{w.titleContent.text} ({w.GetType().Name})"));

                _groups.Add(new WindowGroupData
                {
                    Key = key,
                    DefaultLabel = first.titleContent.text,
                    Tooltip = tooltip,
                    FirstWindow = first,
                    TabCount = windows.Count
                });
            }

            // 順序を適用
            _groups = _groups
                .OrderBy(g =>
                {
                    int idx = _order.IndexOf(g.Key);
                    return idx >= 0 ? idx : int.MaxValue;
                })
                .ThenBy(g => GetDisplayName(g))
                .ToList();
        }

        private void RebuildList()
        {
            _listContainer.Clear();

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                int index = i;
                string displayName = GetDisplayName(g);

                var row = CreateRow(g, index, displayName);
                _listContainer.Add(row);
            }
        }

        private VisualElement CreateRow(WindowGroupData g, int index, string displayName)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 24;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.marginTop = 0;
            row.style.marginBottom = 0;
            row.userData = g;

            // Hover highlight
            row.RegisterCallback<MouseEnterEvent>(evt =>
            {
                row.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f, 1f);
            });
            row.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                row.style.backgroundColor = Color.clear;
            });

            // Name label
            var nameLabel = new Label(displayName);
            nameLabel.style.flexGrow = 1;
            nameLabel.style.fontSize = 11;
            nameLabel.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.pickingMode = PickingMode.Ignore;

            // Count label
            var countLabel = new Label(g.TabCount.ToString());
            countLabel.style.fontSize = 10;
            countLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            countLabel.style.width = 20;
            countLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            countLabel.pickingMode = PickingMode.Ignore;

            row.Add(nameLabel);
            row.Add(countLabel);
            row.tooltip = g.Tooltip;

            // Interactions
            RegisterRowEvents(row, nameLabel, g, index);

            return row;
        }

        // ─────────────────────────────────────────────
        // Interactions: Click, DoubleClick, Drag
        // ─────────────────────────────────────────────

        private void RegisterRowEvents(VisualElement row, Label nameLabel, WindowGroupData g, int index)
        {
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;

                // ダブルクリック → インラインリネーム
                if (evt.clickCount == 2)
                {
                    evt.StopImmediatePropagation();
                    _dragStarted = false;
                    _isDragging = false;
                    _pendingClickSchedule?.Pause();
                    _pendingClickSchedule = null;
                    StartInlineRename(nameLabel, g);
                    return;
                }

                if (_isDragging) return;

                _dragStarted = true;
                _dragStartPos = evt.position;
                _dragIndex = index;
                row.CapturePointer(evt.pointerId);
            }, TrickleDown.TrickleDown);

            row.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_dragStarted && !_isDragging) return;

                var delta = (Vector2)evt.position - _dragStartPos;
                if (!_isDragging)
                {
                    if (delta.magnitude < DragThreshold) return;
                    _isDragging = true;
                    BeginDrag(row);
                }

                if (_isDragging)
                {
                    UpdateDrag(evt.position);
                }
            }, TrickleDown.TrickleDown);

            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (row.HasPointerCapture(evt.pointerId))
                    row.ReleasePointer(evt.pointerId);

                if (_isDragging)
                {
                    EndDrag();
                }
                else if (_dragStarted)
                {
                    // 遅延クリック（ダブルクリック検出猶予）
                    var captured = g;
                    var scheduled = _listContainer.schedule.Execute(() =>
                    {
                        _pendingClickSchedule = null;
                        if (captured.FirstWindow != null)
                        {
                            captured.FirstWindow.Focus();
#if UNITY_EDITOR_WIN
                            _topMostApplied = false;
                            ApplyTopMost();
#endif
                        }
                    });
                    scheduled.ExecuteLater(250);
                    _pendingClickSchedule = scheduled;
                }

                _dragStarted = false;
                _isDragging = false;
            }, TrickleDown.TrickleDown);

            row.RegisterCallback<PointerCaptureOutEvent>(evt =>
            {
                if (evt.target != row) return;
                if (_isDragging) CleanupDrag();
                _dragStarted = false;
                _isDragging = false;
            });
        }

        // ─────────────────────────────────────────────
        // Inline Rename (UIToolkit TextField)
        // ─────────────────────────────────────────────

        private void StartInlineRename(Label nameLabel, WindowGroupData g)
        {
            var parent = nameLabel.parent;
            int idx = parent.IndexOf(nameLabel);

            string currentName = GetDisplayName(g);
            var textField = new TextField();
            textField.value = currentName;
            textField.style.flexGrow = 1;
            textField.style.fontSize = 11;
            textField.style.height = 20;

            parent.Insert(idx, textField);
            nameLabel.style.display = DisplayStyle.None;

            // 次フレームでFocus
            textField.schedule.Execute(() =>
            {
                textField.Focus();
                textField.SelectAll();
            });

            Action commitRename = () =>
            {
                string newName = textField.value;
                if (!string.IsNullOrEmpty(newName))
                {
                    _customNames[g.Key] = newName;
                    SaveCustomNames();
                }
                if (parent.Contains(textField))
                    parent.Remove(textField);
                nameLabel.text = GetDisplayName(g);
                nameLabel.style.display = DisplayStyle.Flex;
            };

            Action cancelRename = () =>
            {
                if (parent.Contains(textField))
                    parent.Remove(textField);
                nameLabel.style.display = DisplayStyle.Flex;
            };

            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    commitRename();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    cancelRename();
                    evt.StopPropagation();
                }
            });

            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (textField.parent != null)
                {
                    commitRename();
                }
            });
        }

        // ─────────────────────────────────────────────
        // Drag & Drop (Reorder)
        // ─────────────────────────────────────────────

        private void BeginDrag(VisualElement sourceRow)
        {
            sourceRow.style.opacity = 0.4f;

            // ドロップインジケータ
            _dropIndicator = new VisualElement();
            _dropIndicator.style.height = 2;
            _dropIndicator.style.backgroundColor = new Color(0.31f, 0.76f, 0.97f);
            _dropIndicator.style.position = Position.Absolute;
            _dropIndicator.style.left = 4;
            _dropIndicator.style.right = 4;
            _dropIndicator.style.display = DisplayStyle.None;

            // 左端マーカー
            var marker = new VisualElement();
            marker.style.position = Position.Absolute;
            marker.style.left = -3;
            marker.style.top = -3;
            marker.style.width = 8;
            marker.style.height = 8;
            marker.style.borderTopLeftRadius = 4;
            marker.style.borderTopRightRadius = 4;
            marker.style.borderBottomLeftRadius = 4;
            marker.style.borderBottomRightRadius = 4;
            marker.style.backgroundColor = new Color(0.31f, 0.76f, 0.97f);
            _dropIndicator.Add(marker);

            _listContainer.Add(_dropIndicator);
        }

        private void UpdateDrag(Vector2 pointerPos)
        {
            if (_dropIndicator == null) return;

            // 最も近い行間を探す
            float bestDist = float.MaxValue;
            float bestY = 0;
            int bestInsertIndex = _dragIndex;

            for (int i = 0; i <= _groups.Count; i++)
            {
                float gapY;
                if (i < _listContainer.childCount - 1) // -1 for dropIndicator
                {
                    var child = _listContainer[i];
                    if (child == _dropIndicator) continue;
                    gapY = child.worldBound.yMin;
                }
                else
                {
                    // 最後の要素の下
                    var lastIdx = _listContainer.childCount - 2; // -1 for dropIndicator
                    if (lastIdx >= 0)
                        gapY = _listContainer[lastIdx].worldBound.yMax;
                    else
                        break;
                }

                float dist = Mathf.Abs(pointerPos.y - gapY);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestY = gapY;
                    bestInsertIndex = i;
                }
            }

            if (bestDist < 100f)
            {
                float localY = bestY - _listContainer.worldBound.yMin;
                _dropIndicator.style.display = DisplayStyle.Flex;
                _dropIndicator.style.top = localY - 1;
                _dropIndicator.userData = bestInsertIndex;
            }
            else
            {
                _dropIndicator.style.display = DisplayStyle.None;
            }
        }

        private void EndDrag()
        {
            if (_dropIndicator != null && _dropIndicator.userData is int targetIndex)
            {
                if (targetIndex != _dragIndex && targetIndex != _dragIndex + 1)
                {
                    // 並び替え実行
                    var item = _groups[_dragIndex];
                    _groups.RemoveAt(_dragIndex);
                    int insertAt = targetIndex > _dragIndex ? targetIndex - 1 : targetIndex;
                    insertAt = Mathf.Clamp(insertAt, 0, _groups.Count);
                    _groups.Insert(insertAt, item);

                    // 順序を保存
                    _order = _groups.Select(g => g.Key).ToList();
                    SaveOrder();
                    RebuildList();
                }
            }

            CleanupDrag();
        }

        private void CleanupDrag()
        {
            // ソース行のopacityリセット
            foreach (var child in _listContainer.Children())
            {
                if (child != _dropIndicator)
                    child.style.opacity = 1f;
            }

            if (_dropIndicator != null)
            {
                _dropIndicator.RemoveFromHierarchy();
                _dropIndicator = null;
            }
        }

        // ─────────────────────────────────────────────
        // Display Name (永続)
        // ─────────────────────────────────────────────

        private string GetDisplayName(WindowGroupData g)
        {
            if (_customNames.TryGetValue(g.Key, out string custom) && !string.IsNullOrEmpty(custom))
                return custom;
            return g.DefaultLabel;
        }

        // ─────────────────────────────────────────────
        // Size
        // ─────────────────────────────────────────────

        private void AdjustWindowSize()
        {
            float rowHeight = 24;
            float padding = 8;
            float minHeight = 32;
            float newHeight = Mathf.Max(minHeight, _groups.Count * rowHeight + padding);

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
                        _customNames[data.Keys[i]] = data.Values[i];
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

        private void LoadOrder()
        {
            _order.Clear();
            string json = EditorPrefs.GetString(OrderPrefsKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<OrderStore>(json);
                if (data != null && data.Keys != null)
                    _order = data.Keys;
            }
            catch { }
        }

        private void SaveOrder()
        {
            var data = new OrderStore { Keys = _order };
            EditorPrefs.SetString(OrderPrefsKey, JsonUtility.ToJson(data));
        }

        [Serializable]
        private class NameStore
        {
            public List<string> Keys = new List<string>();
            public List<string> Values = new List<string>();
        }

        [Serializable]
        private class OrderStore
        {
            public List<string> Keys = new List<string>();
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
