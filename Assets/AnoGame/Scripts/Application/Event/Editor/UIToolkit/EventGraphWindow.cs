#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnoGame.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoFlow.Editor
{
    public class EventGraphWindow : EditorWindow
    {
        private const string PREF_ROOT_PATH = "AnoFlow.EventGraph.RootPath";
        private const string PREF_SELECTED_FOLDER = "AnoFlow.EventGraph.SelectedFolder";
        private const string DEFAULT_ROOT_PATH = "Assets/AnoGame/Data/Events";
        private const string ITEMS_JSON_PATH = "Assets/AnoGame/Data/ItemsResources/items_batch.json";

        private EventGraphView _graphView;
        private List<EventData> _eventDataList;
        private HashSet<string> _knownItemIds;
        private Dictionary<string, string> _itemNameMap;
        private bool _isEditMode = false;

        // Floating panel toggle references
        private Button _allBtn;
        private Toggle _resultToggle;
        private Toggle _conditionToggle;
        private Toggle _eventsToggle;
        private Toggle _itemsToggle;

        // Folder selector references
        private VisualElement _folderListContainer;

        /// <summary>
        /// 現在のルートパス
        /// </summary>
        private string RootPath
        {
            get => EditorPrefs.GetString(PREF_ROOT_PATH, DEFAULT_ROOT_PATH);
            set => EditorPrefs.SetString(PREF_ROOT_PATH, value);
        }

        /// <summary>
        /// 現在選択中のサブフォルダ名
        /// </summary>
        private string SelectedFolder
        {
            get => EditorPrefs.GetString(PREF_SELECTED_FOLDER, "");
            set => EditorPrefs.SetString(PREF_SELECTED_FOLDER, value);
        }

        /// <summary>
        /// 現在のアクティブフォルダのフルパス
        /// </summary>
        private string ActiveFolderPath
        {
            get
            {
                string folder = SelectedFolder;
                if (string.IsNullOrEmpty(folder))
                    return RootPath;
                return $"{RootPath}/{folder}";
            }
        }

        [MenuItem("AnoGame/AnoFlow/Event Graph")]
        public static void Open()
        {
            var window = GetWindow<EventGraphWindow>("Event Graph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            // 選択フォルダが無効ならデフォルトのサブフォルダを選択
            EnsureValidSelectedFolder();
            CleanupSoftDeletedAssets();
            LoadData();
            BuildUI();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (_graphView != null)
            {
                _graphView.OnGraphDataChanged = null;
                _graphView.OnExpandAllStateChanged = null;
            }
            CleanupSoftDeletedAssets();
        }

        private void OnUndoRedo()
        {
            if (_graphView != null && _eventDataList != null)
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
        }

        /// <summary>
        /// 選択中のフォルダが存在するか確認し、無効なら最初のサブフォルダを選択
        /// </summary>
        private void EnsureValidSelectedFolder()
        {
            string root = RootPath;
            if (!Directory.Exists(root)) return;

            string selected = SelectedFolder;
            if (!string.IsNullOrEmpty(selected) && Directory.Exists($"{root}/{selected}"))
                return;

            // 最初のサブフォルダを自動選択
            var subfolders = GetSubfolders();
            if (subfolders.Count > 0)
                SelectedFolder = subfolders[0];
            else
                SelectedFolder = "";
        }

        /// <summary>
        /// ルートパス配下のサブフォルダ名一覧を取得
        /// </summary>
        private List<string> GetSubfolders()
        {
            string root = RootPath;
            var result = new List<string>();
            if (!Directory.Exists(root)) return result;

            var dirs = Directory.GetDirectories(root);
            foreach (var dir in dirs)
            {
                string name = Path.GetFileName(dir);
                // .meta ファイルやUnity隠しフォルダをスキップ
                if (name.StartsWith(".")) continue;
                result.Add(name);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private void CleanupSoftDeletedAssets()
        {
            string folderPath = ActiveFolderPath;
            if (!AssetDatabase.IsValidFolder(folderPath)) return;

            var guids = AssetDatabase.FindAssets("t:EventData", new[] { folderPath });
            int deletedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (asset != null && asset.IsDeleted)
                {
                    var meta = EventGraphMeta.Load(folderPath);
                    var assetGuid = AssetDatabase.AssetPathToGUID(path);
                    meta.RemoveNodePosition(assetGuid);
                    meta.Save(folderPath);
                    AssetDatabase.DeleteAsset(path);
                    deletedCount++;
                }
            }
            if (deletedCount > 0)
            {
                Debug.Log($"Event Graph: {deletedCount} 個のソフトデリート済みアセットをクリーンアップしました");
                AssetDatabase.Refresh();
            }
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/AnoGame/Scripts/Application/Event/Editor/UIToolkit/EventGraphStyles.uss");
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            var root = new VisualElement();
            root.AddToClassList("event-graph-root");
            rootVisualElement.Add(root);

            // === Toolbar ===
            var toolbar = new Toolbar();
            toolbar.AddToClassList("event-toolbar");

            toolbar.Add(new ToolbarButton(() =>
            {
                LoadData();
                _graphView?.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
                SyncTogglesFromSectionVis();
            })
            { text = "Reload" });

            toolbar.Add(new ToolbarButton(() => _graphView?.AutoLayout()) { text = "Auto Layout" });
            toolbar.Add(new ToolbarButton(() => _graphView?.SaveNodePositions()) { text = "Save Positions" });

            root.Add(toolbar);

            // === Graph View container ===
            var graphContainer = new VisualElement();
            graphContainer.style.flexGrow = 1;
            graphContainer.style.position = Position.Relative;

            _graphView = new EventGraphView();
            _graphView.ActiveFolderPath = ActiveFolderPath;
            _graphView.AddToClassList("event-graph-view");
            graphContainer.Add(_graphView);

            // === Floating Panel (vertical layout) ===
            graphContainer.Add(BuildFloatingPanel());

            root.Add(graphContainer);

            if (_eventDataList != null && _eventDataList.Count > 0)
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
                _graphView.SetEditMode(_isEditMode);
            }

            _graphView.OnExpandAllStateChanged = allExpanded =>
            {
                if (!allExpanded)
                    _allBtn?.RemoveFromClassList("expand-all-active");
            };

            SyncTogglesFromSectionVis();
        }

        private VisualElement BuildFloatingPanel()
        {
            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.top = 8;
            panel.style.right = 8;
            panel.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 0.92f));
            panel.style.borderTopLeftRadius = 6;
            panel.style.borderTopRightRadius = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius = 6;
            panel.style.paddingLeft = 8;
            panel.style.paddingRight = 8;
            panel.style.paddingTop = 6;
            panel.style.paddingBottom = 6;
            panel.style.borderTopWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderRightWidth = 1;
            var borderColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            panel.style.borderTopColor = borderColor;
            panel.style.borderBottomColor = borderColor;
            panel.style.borderLeftColor = borderColor;
            panel.style.borderRightColor = borderColor;
            panel.style.minWidth = 140;

            // === Folder Selector Section ===
            var folderHeader = new VisualElement();
            folderHeader.style.flexDirection = FlexDirection.Row;
            folderHeader.style.justifyContent = Justify.SpaceBetween;
            folderHeader.style.alignItems = Align.Center;
            folderHeader.style.marginBottom = 4;

            var folderLabel = new Label("Folder");
            folderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            folderLabel.style.fontSize = 11;
            folderHeader.Add(folderLabel);

            // 設定ボタン
            var settingsBtn = new Button(() => ShowRootPathPopup()) { text = "..." };
            settingsBtn.tooltip = "ルートパスの変更";
            settingsBtn.style.width = 24;
            settingsBtn.style.height = 18;
            settingsBtn.style.fontSize = 10;
            settingsBtn.style.paddingLeft = 0;
            settingsBtn.style.paddingRight = 0;
            settingsBtn.style.paddingTop = 0;
            settingsBtn.style.paddingBottom = 0;
            folderHeader.Add(settingsBtn);

            panel.Add(folderHeader);

            // フォルダ一覧コンテナ
            _folderListContainer = new VisualElement();
            RebuildFolderList();
            panel.Add(_folderListContainer);

            // --- Separator (folder → toggles) ---
            var sep0 = new VisualElement();
            sep0.style.height = 1;
            sep0.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            sep0.style.marginTop = 4;
            sep0.style.marginBottom = 4;
            panel.Add(sep0);

            // --- ALL button ---
            _allBtn = new Button() { text = "ALL" };
            _allBtn.tooltip = "全セクションの展開/折畳をトグル";
            _allBtn.style.height = 22;
            _allBtn.style.marginBottom = 4;
            _allBtn.clicked += OnAllClicked;
            panel.Add(_allBtn);

            // --- Separator ---
            var sep1 = new VisualElement();
            sep1.style.height = 1;
            sep1.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            sep1.style.marginTop = 2;
            sep1.style.marginBottom = 4;
            panel.Add(sep1);

            // --- Section toggles (vertical) ---
            _resultToggle = CreatePanelToggle("Result", "全ノードの Result Tags 表示切替");
            _resultToggle.RegisterValueChangedCallback(_ => OnSectionToggleChanged());
            panel.Add(_resultToggle);

            _conditionToggle = CreatePanelToggle("Condition", "全ノードの Condition Tags 表示切替");
            _conditionToggle.RegisterValueChangedCallback(_ => OnSectionToggleChanged());
            panel.Add(_conditionToggle);

            _eventsToggle = CreatePanelToggle("Events", "全ノードの Req Events 表示切替");
            _eventsToggle.RegisterValueChangedCallback(_ => OnSectionToggleChanged());
            panel.Add(_eventsToggle);

            _itemsToggle = CreatePanelToggle("Items", "全ノードの Req Items 表示切替");
            _itemsToggle.RegisterValueChangedCallback(_ => OnSectionToggleChanged());
            panel.Add(_itemsToggle);

            // --- Separator ---
            var sep2 = new VisualElement();
            sep2.style.height = 1;
            sep2.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            sep2.style.marginTop = 4;
            sep2.style.marginBottom = 4;
            panel.Add(sep2);

            // --- Negative toggle ---
            var negativeToggle = CreatePanelToggle("Negative", "ネガティブタグ(!)の\n抑制ラインを表示");
            negativeToggle.RegisterValueChangedCallback(evt =>
            {
                _graphView?.SetShowNegativeEdges(evt.newValue);
            });
            panel.Add(negativeToggle);

            // --- Edit toggle (right-aligned row) ---
            var editRow = new VisualElement();
            editRow.style.flexDirection = FlexDirection.Row;
            editRow.style.justifyContent = Justify.FlexEnd;

            var editToggle = CreatePanelToggle("Edit", "編集モードのON/OFF");
            editToggle.value = _isEditMode;
            editToggle.RegisterValueChangedCallback(evt =>
            {
                _isEditMode = evt.newValue;
                _graphView?.SetEditMode(_isEditMode);
            });
            editRow.Add(editToggle);
            panel.Add(editRow);

            return panel;
        }

        /// <summary>
        /// フォルダ一覧を再構築する
        /// </summary>
        private void RebuildFolderList()
        {
            if (_folderListContainer == null) return;
            _folderListContainer.Clear();

            var subfolders = GetSubfolders();
            string selected = SelectedFolder;

            if (subfolders.Count == 0)
            {
                var noFolderLabel = new Label("(no subfolders)");
                noFolderLabel.style.fontSize = 10;
                noFolderLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                _folderListContainer.Add(noFolderLabel);
                return;
            }

            foreach (var folder in subfolders)
            {
                var btn = new Button(() => OnFolderSelected(folder));
                btn.text = folder;
                btn.style.height = 20;
                btn.style.fontSize = 11;
                btn.style.marginTop = 1;
                btn.style.marginBottom = 1;
                btn.style.paddingLeft = 6;
                btn.style.paddingRight = 6;
                btn.style.unityTextAlign = TextAnchor.MiddleLeft;

                if (folder == selected)
                {
                    btn.style.backgroundColor = new StyleColor(new Color(0.24f, 0.49f, 0.91f, 0.7f));
                    btn.style.color = new StyleColor(Color.white);
                }

                _folderListContainer.Add(btn);
            }
        }

        /// <summary>
        /// フォルダ選択時の処理
        /// </summary>
        private void OnFolderSelected(string folderName)
        {
            if (folderName == SelectedFolder) return;

            SelectedFolder = folderName;

            // GraphView のアクティブパスを更新
            if (_graphView != null)
                _graphView.ActiveFolderPath = ActiveFolderPath;

            // リロード
            CleanupSoftDeletedAssets();
            LoadData();
            _graphView?.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
            SyncTogglesFromSectionVis();

            // フォルダ一覧のハイライト更新
            RebuildFolderList();
        }

        /// <summary>
        /// ルートパス変更ポップアップを表示
        /// </summary>
        private void ShowRootPathPopup()
        {
            string currentRoot = RootPath;
            string selected = EditorUtility.OpenFolderPanel("イベントデータのルートフォルダを選択", currentRoot, "");

            if (string.IsNullOrEmpty(selected)) return;

            // 絶対パスを Assets/ 相対パスに変換
            string dataPath = UnityEngine.Application.dataPath;
            if (selected.StartsWith(dataPath))
            {
                selected = "Assets" + selected.Substring(dataPath.Length);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Assets フォルダ外のパスは指定できません。", "OK");
                return;
            }

            // パス区切り文字を統一
            selected = selected.Replace("\\", "/");

            RootPath = selected;
            SelectedFolder = "";
            EnsureValidSelectedFolder();

            // GraphView のアクティブパスを更新
            if (_graphView != null)
                _graphView.ActiveFolderPath = ActiveFolderPath;

            CleanupSoftDeletedAssets();
            LoadData();
            _graphView?.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
            SyncTogglesFromSectionVis();
            RebuildFolderList();
        }

        /// <summary>
        /// パネル用トグル。テキスト左、チェックボックス右揃え。
        /// </summary>
        private static Toggle CreatePanelToggle(string label, string tooltip)
        {
            var toggle = new Toggle(label);
            toggle.tooltip = tooltip;
            toggle.style.flexDirection = FlexDirection.RowReverse;
            toggle.style.justifyContent = Justify.SpaceBetween;
            toggle.style.marginTop = 1;
            toggle.style.marginBottom = 1;
            return toggle;
        }

        private void OnAllClicked()
        {
            if (_graphView == null) return;

            bool allOn = _resultToggle.value && _conditionToggle.value
                      && _eventsToggle.value && _itemsToggle.value;
            bool expand = !allOn;

            _resultToggle.SetValueWithoutNotify(expand);
            _conditionToggle.SetValueWithoutNotify(expand);
            _eventsToggle.SetValueWithoutNotify(expand);
            _itemsToggle.SetValueWithoutNotify(expand);

            _graphView.SectionVis.ResultTags = expand;
            _graphView.SectionVis.ConditionTags = expand;
            _graphView.SectionVis.RequiredEvents = expand;
            _graphView.SectionVis.RequiredItems = expand;

            _graphView.RebuildForSectionChange();
            UpdateAllHighlight();
        }

        private void OnSectionToggleChanged()
        {
            if (_graphView == null) return;

            _graphView.SectionVis.ResultTags = _resultToggle.value;
            _graphView.SectionVis.ConditionTags = _conditionToggle.value;
            _graphView.SectionVis.RequiredEvents = _eventsToggle.value;
            _graphView.SectionVis.RequiredItems = _itemsToggle.value;

            _graphView.RebuildForSectionChange();
            UpdateAllHighlight();
        }

        private void SyncTogglesFromSectionVis()
        {
            if (_graphView == null) return;
            _resultToggle?.SetValueWithoutNotify(_graphView.SectionVis.ResultTags);
            _conditionToggle?.SetValueWithoutNotify(_graphView.SectionVis.ConditionTags);
            _eventsToggle?.SetValueWithoutNotify(_graphView.SectionVis.RequiredEvents);
            _itemsToggle?.SetValueWithoutNotify(_graphView.SectionVis.RequiredItems);
            UpdateAllHighlight();
        }

        private void UpdateAllHighlight()
        {
            if (_allBtn == null) return;
            bool allOn = _resultToggle != null && _resultToggle.value
                      && _conditionToggle != null && _conditionToggle.value
                      && _eventsToggle != null && _eventsToggle.value
                      && _itemsToggle != null && _itemsToggle.value;
            if (allOn)
                _allBtn.AddToClassList("expand-all-active");
            else
                _allBtn.RemoveFromClassList("expand-all-active");
        }

        private void LoadData()
        {
            string folderPath = ActiveFolderPath;
            _eventDataList = new List<EventData>();

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"[EventGraph] LoadData: フォルダが存在しません: {folderPath}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:EventData", new[] { folderPath });
            int skippedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (asset != null)
                {
                    if (asset.IsDeleted)
                    {
                        Debug.Log($"[EventGraph] LoadData: isDeleted=true でスキップ: {asset.EventId}, path={path}");
                        skippedCount++;
                    }
                    else
                    {
                        _eventDataList.Add(asset);
                    }
                }
            }
            Debug.Log($"[EventGraph] LoadData: フォルダ={folderPath}, ディスク上{guids.Length}件, スキップ{skippedCount}件, 読み込み{_eventDataList.Count}件");
            _eventDataList.Sort((a, b) => string.Compare(a.EventId, b.EventId, StringComparison.Ordinal));

            _knownItemIds = new HashSet<string>();
            _itemNameMap = new Dictionary<string, string>();
            if (File.Exists(ITEMS_JSON_PATH))
            {
                try
                {
                    string json = File.ReadAllText(ITEMS_JSON_PATH);
                    var itemList = JsonUtility.FromJson<ItemListWrapper>("{\"items\":" + json + "}");
                    if (itemList?.items != null)
                    {
                        foreach (var item in itemList.items)
                        {
                            if (!string.IsNullOrEmpty(item.id))
                            {
                                _knownItemIds.Add(item.id);
                                if (!string.IsNullOrEmpty(item.name))
                                    _itemNameMap[item.id] = item.name;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load items JSON: {ex.Message}");
                }
            }

            Debug.Log($"Event Graph: Loaded {_eventDataList.Count} EventData assets, {_knownItemIds.Count} known items.");
        }

        [Serializable]
        private class ItemListWrapper
        {
            public List<ItemEntry> items;
        }

        [Serializable]
        private class ItemEntry
        {
            public string id;
            public string name;
        }
    }
}
#endif
