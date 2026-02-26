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
        private const string EVENTDATA_DIR_PATH = "Assets/AnoGame/Data/Events/Story2";
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

        [MenuItem("AnoGame/AnoFlow/Event Graph")]
        public static void Open()
        {
            var window = GetWindow<EventGraphWindow>("Event Graph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
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
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
            }
        }

        private void CleanupSoftDeletedAssets()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EVENTDATA_DIR_PATH });
            int deletedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (asset != null && asset.IsDeleted)
                {
                    var meta = EventGraphMeta.Load();
                    var assetGuid = AssetDatabase.AssetPathToGUID(path);
                    meta.RemoveNodePosition(assetGuid);
                    meta.Save();
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

            // === Toolbar (left: Reload, Auto Layout, Save Positions) ===
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
            _graphView.AddToClassList("event-graph-view");
            graphContainer.Add(_graphView);

            // === Floating Panel ===
            var panel = BuildFloatingPanel();
            graphContainer.Add(panel);

            root.Add(graphContainer);

            if (_eventDataList != null && _eventDataList.Count > 0)
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
                _graphView.SetEditMode(_isEditMode);
            }

            // ノードのヘッダークリック折りたたみ → ALLハイライト解除
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
            panel.style.paddingLeft = 6;
            panel.style.paddingRight = 6;
            panel.style.paddingTop = 4;
            panel.style.paddingBottom = 4;
            panel.style.borderTopWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderRightWidth = 1;
            var borderColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            panel.style.borderTopColor = borderColor;
            panel.style.borderBottomColor = borderColor;
            panel.style.borderLeftColor = borderColor;
            panel.style.borderRightColor = borderColor;

            // --- Row 1: ALL + Section toggles ---
            var row1 = new VisualElement();
            row1.style.flexDirection = FlexDirection.Row;
            row1.style.alignItems = Align.Center;
            row1.style.marginBottom = 4;

            _allBtn = new Button() { text = "ALL" };
            _allBtn.tooltip = "\u5168\u30bb\u30af\u30b7\u30e7\u30f3\u306e\u5c55\u958b/\u6298\u7573\u3092\u30c8\u30b0\u30eb";
            _allBtn.clicked += OnAllClicked;
            StyleFloatingButton(_allBtn);
            row1.Add(_allBtn);

            AddSpacer(row1, 8);

            _resultToggle = CreateSectionToggle("Result", "\u5168\u30ce\u30fc\u30c9\u306e Result Tags \u8868\u793a\u5207\u66ff");
            row1.Add(_resultToggle);

            _conditionToggle = CreateSectionToggle("Condition", "\u5168\u30ce\u30fc\u30c9\u306e Condition Tags \u8868\u793a\u5207\u66ff");
            row1.Add(_conditionToggle);

            _eventsToggle = CreateSectionToggle("Events", "\u5168\u30ce\u30fc\u30c9\u306e Req Events \u8868\u793a\u5207\u66ff");
            row1.Add(_eventsToggle);

            _itemsToggle = CreateSectionToggle("Items", "\u5168\u30ce\u30fc\u30c9\u306e Req Items \u8868\u793a\u5207\u66ff");
            row1.Add(_itemsToggle);

            panel.Add(row1);

            // --- Row 2: Negative (left) ... Edit (right) ---
            var row2 = new VisualElement();
            row2.style.flexDirection = FlexDirection.Row;
            row2.style.alignItems = Align.Center;

            var negativeToggle = new Toggle() { text = "Negative", value = false };
            negativeToggle.tooltip = "\u30cd\u30ac\u30c6\u30a3\u30d6\u30bf\u30b0(!)\u306e\n\u6291\u5236\u30e9\u30a4\u30f3\u3092\u8868\u793a";
            negativeToggle.RegisterValueChangedCallback(evt =>
            {
                _graphView?.SetShowNegativeEdges(evt.newValue);
            });
            row2.Add(negativeToggle);

            // Spacer to push Edit to right
            var editSpacer = new VisualElement();
            editSpacer.style.flexGrow = 1;
            row2.Add(editSpacer);

            var editToggle = new Toggle() { text = "Edit", value = _isEditMode };
            editToggle.tooltip = "\u7de8\u96c6\u30e2\u30fc\u30c9\u306eON/OFF";
            editToggle.RegisterValueChangedCallback(evt =>
            {
                _isEditMode = evt.newValue;
                _graphView?.SetEditMode(_isEditMode);
            });
            row2.Add(editToggle);

            panel.Add(row2);

            return panel;
        }

        private Toggle CreateSectionToggle(string label, string tooltip)
        {
            var toggle = new Toggle() { text = label, value = false };
            toggle.tooltip = tooltip;
            toggle.style.marginLeft = 4;
            toggle.RegisterValueChangedCallback(evt => OnSectionToggleChanged());
            return toggle;
        }

        /// <summary>
        /// ALL ボタンクリック: 全トグルを一括ON/OFF。
        /// </summary>
        private void OnAllClicked()
        {
            if (_graphView == null) return;

            bool allOn = _resultToggle.value && _conditionToggle.value
                      && _eventsToggle.value && _itemsToggle.value;
            bool expand = !allOn;

            // トグルUI更新（コールバック発火を防ぐ）
            _resultToggle.SetValueWithoutNotify(expand);
            _conditionToggle.SetValueWithoutNotify(expand);
            _eventsToggle.SetValueWithoutNotify(expand);
            _itemsToggle.SetValueWithoutNotify(expand);

            // SectionVis 更新
            _graphView.SectionVis.ResultTags = expand;
            _graphView.SectionVis.ConditionTags = expand;
            _graphView.SectionVis.RequiredEvents = expand;
            _graphView.SectionVis.RequiredItems = expand;

            _graphView.RebuildForSectionChange();
            UpdateAllHighlight();
        }

        /// <summary>
        /// 個別セクショントグル変更時: SectionVis 更新 + リビルド + ALLハイライト更新。
        /// </summary>
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

        /// <summary>
        /// SectionVis の現在値をトグルUIに反映する（Reload後など）。
        /// </summary>
        private void SyncTogglesFromSectionVis()
        {
            if (_graphView == null) return;
            _resultToggle?.SetValueWithoutNotify(_graphView.SectionVis.ResultTags);
            _conditionToggle?.SetValueWithoutNotify(_graphView.SectionVis.ConditionTags);
            _eventsToggle?.SetValueWithoutNotify(_graphView.SectionVis.RequiredEvents);
            _itemsToggle?.SetValueWithoutNotify(_graphView.SectionVis.RequiredItems);
            UpdateAllHighlight();
        }

        /// <summary>
        /// ALLハイライト: 全トグルONならハイライト。
        /// </summary>
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

        private static void StyleFloatingButton(Button btn)
        {
            btn.style.height = 20;
            btn.style.fontSize = 11;
            btn.style.marginLeft = 2;
            btn.style.marginRight = 2;
            btn.style.paddingLeft = 6;
            btn.style.paddingRight = 6;
        }

        private static void AddSpacer(VisualElement parent, float width)
        {
            var spacer = new VisualElement();
            spacer.style.width = width;
            parent.Add(spacer);
        }

        private void LoadData()
        {
            _eventDataList = new List<EventData>();
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EVENTDATA_DIR_PATH });
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
            Debug.Log($"[EventGraph] LoadData: ディスク上{guids.Length}件, スキップ{skippedCount}件, 読み込み{_eventDataList.Count}件");
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
