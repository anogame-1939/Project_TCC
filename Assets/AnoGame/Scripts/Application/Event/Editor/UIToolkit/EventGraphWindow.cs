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
            }
            CleanupSoftDeletedAssets();
        }

        private void OnUndoRedo()
        {
            // Undo/Redo 後にグラフを再構築（isDeleted の変更が反映される）
            if (_graphView != null && _eventDataList != null)
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
            }
        }

        /// <summary>
        /// isDeleted == true の EventData アセットを実際に削除する
        /// </summary>
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
                    // meta.json からも除去
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

            // Load USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/AnoGame/Scripts/Application/Event/Editor/UIToolkit/EventGraphStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            var root = new VisualElement();
            root.AddToClassList("event-graph-root");
            rootVisualElement.Add(root);

            // Toolbar (left side only: Reload, Auto Layout, Save Positions)
            var toolbar = new Toolbar();
            toolbar.AddToClassList("event-toolbar");

            var reloadBtn = new ToolbarButton(() =>
            {
                LoadData();
                _graphView?.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
            })
            { text = "Reload" };
            toolbar.Add(reloadBtn);

            var autoLayoutBtn = new ToolbarButton(() => _graphView?.AutoLayout()) { text = "Auto Layout" };
            toolbar.Add(autoLayoutBtn);

            var savePositionsBtn = new ToolbarButton(() => _graphView?.SaveNodePositions()) { text = "Save Positions" };
            toolbar.Add(savePositionsBtn);

            root.Add(toolbar);

            // Graph View container (relative positioning for floating panel)
            var graphContainer = new VisualElement();
            graphContainer.style.flexGrow = 1;
            graphContainer.style.position = Position.Relative;

            _graphView = new EventGraphView();
            _graphView.AddToClassList("event-graph-view");
            graphContainer.Add(_graphView);

            // ===== Floating Panel (right-top overlay) =====
            var floatingPanel = new VisualElement();
            floatingPanel.AddToClassList("floating-panel");
            floatingPanel.style.position = Position.Absolute;
            floatingPanel.style.top = 8;
            floatingPanel.style.right = 8;
            floatingPanel.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 0.92f));
            floatingPanel.style.borderTopLeftRadius = 6;
            floatingPanel.style.borderTopRightRadius = 6;
            floatingPanel.style.borderBottomLeftRadius = 6;
            floatingPanel.style.borderBottomRightRadius = 6;
            floatingPanel.style.paddingLeft = 6;
            floatingPanel.style.paddingRight = 6;
            floatingPanel.style.paddingTop = 4;
            floatingPanel.style.paddingBottom = 4;
            floatingPanel.style.borderTopWidth = 1;
            floatingPanel.style.borderBottomWidth = 1;
            floatingPanel.style.borderLeftWidth = 1;
            floatingPanel.style.borderRightWidth = 1;
            floatingPanel.style.borderTopColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            floatingPanel.style.borderBottomColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            floatingPanel.style.borderLeftColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            floatingPanel.style.borderRightColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));

            // Row 1: Section toggles
            var sectionRow = new VisualElement();
            sectionRow.style.flexDirection = FlexDirection.Row;
            sectionRow.style.marginBottom = 4;

            var expandToggleBtn = new Button() { text = "\u25BC \u5168\u5c55\u958b" };
            expandToggleBtn.tooltip = "\u5168\u30bb\u30af\u30b7\u30e7\u30f3\u306e\u5c55\u958b/\u6298\u7573\u3092\u30c8\u30b0\u30eb";
            expandToggleBtn.clicked += () => _graphView?.ToggleAllSections();
            StyleFloatingButton(expandToggleBtn);
            sectionRow.Add(expandToggleBtn);

            Action<bool> onExpandAllChanged = allExpanded =>
            {
                if (allExpanded)
                    expandToggleBtn.AddToClassList("expand-all-active");
                else
                    expandToggleBtn.RemoveFromClassList("expand-all-active");
            };

            AddFloatingSpacer(sectionRow);

            var toggleResult = new Button(() => _graphView?.ToggleSection("ResultTags")) { text = "Result" };
            toggleResult.tooltip = "Result Tags \u8868\u793a\u5207\u66ff";
            StyleFloatingButton(toggleResult);
            sectionRow.Add(toggleResult);

            var toggleCondition = new Button(() => _graphView?.ToggleSection("ConditionTags")) { text = "Condition" };
            toggleCondition.tooltip = "Condition Tags \u8868\u793a\u5207\u66ff";
            StyleFloatingButton(toggleCondition);
            sectionRow.Add(toggleCondition);

            var toggleReqEvents = new Button(() => _graphView?.ToggleSection("RequiredEvents")) { text = "Events" };
            toggleReqEvents.tooltip = "Required Events \u8868\u793a\u5207\u66ff";
            StyleFloatingButton(toggleReqEvents);
            sectionRow.Add(toggleReqEvents);

            var toggleReqItems = new Button(() => _graphView?.ToggleSection("RequiredItems")) { text = "Items" };
            toggleReqItems.tooltip = "Required Items \u8868\u793a\u5207\u66ff";
            StyleFloatingButton(toggleReqItems);
            sectionRow.Add(toggleReqItems);

            floatingPanel.Add(sectionRow);

            // Row 2: Edit / Negative toggles
            var modeRow = new VisualElement();
            modeRow.style.flexDirection = FlexDirection.Row;

            var editToggle = new Toggle() { text = "Edit", value = _isEditMode };
            editToggle.tooltip = "\u7de8\u96c6\u30e2\u30fc\u30c9\u306eON/OFF";
            editToggle.RegisterValueChangedCallback(evt =>
            {
                _isEditMode = evt.newValue;
                _graphView?.SetEditMode(_isEditMode);
            });
            editToggle.style.marginRight = 8;
            modeRow.Add(editToggle);

            var negativeToggle = new Toggle() { text = "Negative", value = false };
            negativeToggle.tooltip = "\u30cd\u30ac\u30c6\u30a3\u30d6\u30bf\u30b0(!)\u306e\n\u6291\u5236\u30e9\u30a4\u30f3\u3092\u8868\u793a";
            negativeToggle.RegisterValueChangedCallback(evt =>
            {
                _graphView?.SetShowNegativeEdges(evt.newValue);
            });
            modeRow.Add(negativeToggle);

            floatingPanel.Add(modeRow);

            graphContainer.Add(floatingPanel);
            root.Add(graphContainer);

            if (_eventDataList != null && _eventDataList.Count > 0)
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
                _graphView.SetEditMode(_isEditMode);
            }

            _graphView.OnExpandAllStateChanged = onExpandAllChanged;
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

        private static void AddFloatingSpacer(VisualElement parent)
        {
            var spacer = new VisualElement();
            spacer.style.width = 8;
            parent.Add(spacer);
        }

        private void LoadData()
        {
            // Load EventData assets from folder
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

            // Load known item IDs from items_batch.json
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
