using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace AnoGame.Utility.Editor
{
    /// <summary>
    /// 選択中の GameObject に対して、最近使用したコンポーネントのワンクリック追加と
    /// 値込みピン留めコンポーネントの復元追加を行う EditorWindow。
    /// </summary>
    public class QuickComponentAdderWindow : EditorWindow
    {
        // ──────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────
        private const int MaxRecentCount = 5;
        private const string PrefKeyRecent = "QuickComponentAdder_Recent";
        private const string PrefKeyPinned = "QuickComponentAdder_Pinned";

        // ──────────────────────────────────────────
        // Data
        // ──────────────────────────────────────────
        [Serializable]
        public class PinnedComponentEntry
        {
            public string assemblyQualifiedTypeName;
            public string displayLabel;
            public string jsonValues;
        }

        [Serializable]
        private class PinnedList
        {
            public List<PinnedComponentEntry> entries = new List<PinnedComponentEntry>();
        }

        private List<string> _recentTypeNames = new List<string>();
        private PinnedList _pinnedList = new PinnedList();
        private bool _initialized;

        // ──────────────────────────────────────────
        // UI Elements
        // ──────────────────────────────────────────
        private VisualElement _recentContent;
        private VisualElement _pinnedContent;
        private Label _noSelectionLabel;

        // ──────────────────────────────────────────
        // Menu
        // ──────────────────────────────────────────
        [MenuItem("Tools/Quick Component Adder")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<QuickComponentAdderWindow>();
            wnd.titleContent = new GUIContent("Quick Add");
            wnd.minSize = new Vector2(220, 150);
        }

        // ──────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────
        private void OnEnable()
        {
            LoadData();
            ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        private void OnDisable()
        {
            ObjectFactory.componentWasAdded -= OnComponentAdded;
        }

        private void CreateGUI()
        {
            BuildUI();
        }

        private void OnSelectionChange()
        {
            // CreateGUI がまだ呼ばれていない場合はスキップ
            if (!_initialized) return;
            RefreshAll();
        }

        // ──────────────────────────────────────────
        // UI Building
        // ──────────────────────────────────────────
        private void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();

            // USS をパスで直接ロード
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/AnoUtility/Editor/QuickComponentAdderWindow.uss");
            if (uss != null)
            {
                root.styleSheets.Add(uss);
            }

            // ── Root padding ──
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;

            // ── No Selection Label ──
            _noSelectionLabel = new Label("GameObject not selected");
            _noSelectionLabel.AddToClassList("no-selection-label");
            root.Add(_noSelectionLabel);

            // ── Recent Section ──
            var recentSection = CreateSection("Recent", out _recentContent);
            root.Add(recentSection);

            // ── Pinned Section ──
            var pinnedSection = CreateSection("Pinned", out _pinnedContent);
            root.Add(pinnedSection);

            _initialized = true;
            RefreshAll();
        }

        private VisualElement CreateSection(string title, out VisualElement content)
        {
            var section = new VisualElement();
            section.AddToClassList("section");
            section.style.marginBottom = 6;
            section.style.borderBottomWidth = 1;
            section.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            section.style.paddingBottom = 4;

            // Header
            var header = new VisualElement();
            header.AddToClassList("section-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.paddingLeft = 6;
            header.style.paddingRight = 6;
            header.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);
            header.style.borderTopLeftRadius = 3;
            header.style.borderTopRightRadius = 3;
            header.style.borderBottomLeftRadius = 3;
            header.style.borderBottomRightRadius = 3;
            header.style.marginBottom = 2;

            var headerLabel = new Label($"\u25bc {title}");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 11;
            headerLabel.style.color = new Color(1f, 1f, 1f, 0.85f);
            header.Add(headerLabel);

            var sectionContent = new VisualElement();
            sectionContent.style.paddingLeft = 2;
            sectionContent.style.paddingRight = 2;

            // Toggle
            header.RegisterCallback<ClickEvent>(_ =>
            {
                bool visible = sectionContent.resolvedStyle.display != DisplayStyle.None;
                sectionContent.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
                headerLabel.text = visible ? $"\u25b6 {title}" : $"\u25bc {title}";
            });

            section.Add(header);
            section.Add(sectionContent);
            content = sectionContent;
            return section;
        }

        private void RefreshAll()
        {
            bool hasSelection = Selection.activeGameObject != null;
            if (_noSelectionLabel != null)
            {
                _noSelectionLabel.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            }

            RefreshRecentUI();
            RefreshPinnedUI();
        }

        private void RefreshRecentUI()
        {
            if (_recentContent == null) return;
            _recentContent.Clear();

            if (_recentTypeNames.Count == 0)
            {
                var info = new Label("No history");
                info.style.paddingTop = 8;
                info.style.paddingBottom = 8;
                info.style.unityTextAlign = TextAnchor.MiddleCenter;
                info.style.fontSize = 11;
                info.style.color = new Color(1f, 1f, 1f, 0.35f);
                info.style.unityFontStyleAndWeight = FontStyle.Italic;
                _recentContent.Add(info);
                return;
            }

            foreach (var typeName in _recentTypeNames)
            {
                var type = FindType(typeName);
                if (type == null) continue;

                var row = CreateComponentRow(
                    $"+ {type.Name}",
                    () => AddComponentToSelection(type),
                    Selection.activeGameObject != null);

                _recentContent.Add(row);
            }
        }

        private void RefreshPinnedUI()
        {
            if (_pinnedContent == null) return;
            _pinnedContent.Clear();

            if (_pinnedList.entries.Count == 0)
            {
                var info = new Label("Right-click a component property\nto pin with values");
                info.style.paddingTop = 8;
                info.style.paddingBottom = 8;
                info.style.unityTextAlign = TextAnchor.MiddleCenter;
                info.style.fontSize = 11;
                info.style.color = new Color(1f, 1f, 1f, 0.35f);
                info.style.unityFontStyleAndWeight = FontStyle.Italic;
                _pinnedContent.Add(info);
                return;
            }

            for (int i = 0; i < _pinnedList.entries.Count; i++)
            {
                var entry = _pinnedList.entries[i];
                int index = i;

                var type = FindType(entry.assemblyQualifiedTypeName);
                string label = type != null
                    ? $"+ {entry.displayLabel}"
                    : $"+ (missing) {entry.displayLabel}";

                var row = CreateComponentRow(
                    label,
                    () => AddPinnedComponentToSelection(entry),
                    Selection.activeGameObject != null && type != null);

                // Remove button
                var removeBtn = new Button(() => RemovePinnedEntry(index));
                removeBtn.text = "\u2715";
                removeBtn.style.width = 20;
                removeBtn.style.height = 20;
                removeBtn.style.marginLeft = 2;
                removeBtn.style.paddingTop = 0;
                removeBtn.style.paddingBottom = 0;
                removeBtn.style.paddingLeft = 0;
                removeBtn.style.paddingRight = 0;
                removeBtn.style.borderTopWidth = 0;
                removeBtn.style.borderBottomWidth = 0;
                removeBtn.style.borderLeftWidth = 0;
                removeBtn.style.borderRightWidth = 0;
                removeBtn.style.borderTopLeftRadius = 3;
                removeBtn.style.borderTopRightRadius = 3;
                removeBtn.style.borderBottomLeftRadius = 3;
                removeBtn.style.borderBottomRightRadius = 3;
                removeBtn.style.backgroundColor = Color.clear;
                removeBtn.style.color = new Color(1f, 1f, 1f, 0.35f);
                removeBtn.style.fontSize = 12;
                removeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                row.Add(removeBtn);

                _pinnedContent.Add(row);
            }
        }

        private VisualElement CreateComponentRow(string label, Action onClick, bool enabled)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;
            row.style.paddingLeft = 4;
            row.style.paddingRight = 4;
            row.style.marginTop = 1;
            row.style.marginBottom = 1;
            row.style.borderTopLeftRadius = 3;
            row.style.borderTopRightRadius = 3;
            row.style.borderBottomLeftRadius = 3;
            row.style.borderBottomRightRadius = 3;

            var btn = new Button(onClick);
            btn.text = label;
            btn.style.flexGrow = 1;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingTop = 3;
            btn.style.paddingBottom = 3;
            btn.style.paddingLeft = 8;
            btn.style.paddingRight = 8;
            btn.style.marginTop = 0;
            btn.style.marginBottom = 0;
            btn.style.marginLeft = 0;
            btn.style.marginRight = 0;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            btn.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            btn.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
            btn.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
            btn.style.borderTopLeftRadius = 3;
            btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3;
            btn.style.borderBottomRightRadius = 3;
            btn.style.backgroundColor = new Color(1f, 1f, 1f, 0.03f);
            btn.style.fontSize = 11;
            btn.style.color = new Color(1f, 1f, 1f, 0.78f);
            btn.SetEnabled(enabled);
            row.Add(btn);

            return row;
        }

        // ──────────────────────────────────────────
        // Actions
        // ──────────────────────────────────────────
        private void AddComponentToSelection(Type componentType)
        {
            var go = Selection.activeGameObject;
            if (go == null || componentType == null) return;

            Undo.AddComponent(go, componentType);
        }

        private void AddPinnedComponentToSelection(PinnedComponentEntry entry)
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            var type = FindType(entry.assemblyQualifiedTypeName);
            if (type == null)
            {
                Debug.LogWarning($"[QuickComponentAdder] Type not found: {entry.assemblyQualifiedTypeName}");
                return;
            }

            var component = Undo.AddComponent(go, type);
            if (component != null && !string.IsNullOrEmpty(entry.jsonValues))
            {
                Undo.RecordObject(component, "Restore Pinned Component Values");
                EditorJsonUtility.FromJsonOverwrite(entry.jsonValues, component);
                EditorUtility.SetDirty(component);
            }
        }

        private void RemovePinnedEntry(int index)
        {
            if (index < 0 || index >= _pinnedList.entries.Count) return;
            _pinnedList.entries.RemoveAt(index);
            SavePinnedData();
            RefreshPinnedUI();
        }

        // ──────────────────────────────────────────
        // Tracking
        // ──────────────────────────────────────────
        private void OnComponentAdded(Component component)
        {
            if (component == null) return;

            var typeName = component.GetType().AssemblyQualifiedName;
            if (string.IsNullOrEmpty(typeName)) return;

            // 重複除去して先頭に追加
            _recentTypeNames.Remove(typeName);
            _recentTypeNames.Insert(0, typeName);

            // 上限カット
            while (_recentTypeNames.Count > MaxRecentCount)
            {
                _recentTypeNames.RemoveAt(_recentTypeNames.Count - 1);
            }

            SaveRecentData();

            if (_initialized)
            {
                RefreshRecentUI();
            }
        }

        // ──────────────────────────────────────────
        // Pinned Entry API (called from ComponentPinContextMenu)
        // ──────────────────────────────────────────
        public static void AddPinnedEntry(PinnedComponentEntry entry)
        {
            var wnd = GetWindow<QuickComponentAdderWindow>();
            wnd.titleContent = new GUIContent("Quick Add");

            wnd._pinnedList.entries.Add(entry);
            wnd.SavePinnedData();

            if (wnd._initialized)
            {
                wnd.RefreshPinnedUI();
            }
        }

        // ──────────────────────────────────────────
        // Persistence
        // ──────────────────────────────────────────
        private void LoadData()
        {
            // Recent
            var recentRaw = EditorPrefs.GetString(PrefKeyRecent, "");
            if (!string.IsNullOrEmpty(recentRaw))
            {
                _recentTypeNames = recentRaw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            // Pinned
            var pinnedRaw = EditorPrefs.GetString(PrefKeyPinned, "");
            if (!string.IsNullOrEmpty(pinnedRaw))
            {
                try
                {
                    _pinnedList = JsonUtility.FromJson<PinnedList>(pinnedRaw) ?? new PinnedList();
                }
                catch
                {
                    _pinnedList = new PinnedList();
                }
            }
        }

        private void SaveRecentData()
        {
            EditorPrefs.SetString(PrefKeyRecent, string.Join("|", _recentTypeNames));
        }

        private void SavePinnedData()
        {
            EditorPrefs.SetString(PrefKeyPinned, JsonUtility.ToJson(_pinnedList));
        }

        // ──────────────────────────────────────────
        // Utilities
        // ──────────────────────────────────────────
        private static Type FindType(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return null;
            return Type.GetType(assemblyQualifiedName);
        }
    }
}
