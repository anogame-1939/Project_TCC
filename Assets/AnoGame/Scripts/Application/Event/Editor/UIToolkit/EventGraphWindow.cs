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

            // Toolbar
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

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Section toggle buttons
            var expandAllBtn = new ToolbarButton(() => _graphView?.ToggleAllSections(true)) { text = "\u25BC 全展開" };
            toolbar.Add(expandAllBtn);

            var collapseAllBtn = new ToolbarButton(() => _graphView?.ToggleAllSections(false)) { text = "\u25B6 全折畳" };
            toolbar.Add(collapseAllBtn);

            toolbar.Add(new ToolbarSpacer());

            var toggleResult = new ToolbarButton(() => _graphView?.ToggleSection("ResultTags")) { text = "Result" };
            toggleResult.tooltip = "Result Tags 表示切替";
            toolbar.Add(toggleResult);

            var toggleCondition = new ToolbarButton(() => _graphView?.ToggleSection("ConditionTags")) { text = "Condition" };
            toggleCondition.tooltip = "Condition Tags 表示切替";
            toolbar.Add(toggleCondition);

            var toggleReqEvents = new ToolbarButton(() => _graphView?.ToggleSection("RequiredEvents")) { text = "Events" };
            toggleReqEvents.tooltip = "Required Events 表示切替";
            toolbar.Add(toggleReqEvents);

            var toggleReqItems = new ToolbarButton(() => _graphView?.ToggleSection("RequiredItems")) { text = "Items" };
            toggleReqItems.tooltip = "Required Items 表示切替";
            toolbar.Add(toggleReqItems);

            toolbar.Add(new ToolbarSpacer());

            // Edit mode toggle
            ToolbarToggle editToggle = null;
            editToggle = new ToolbarToggle()
            {
                text = "Edit",
                value = _isEditMode
            };
            editToggle.tooltip = "編集モードのON/OFF\n+▲▼×ボタンの表示切替";
            editToggle.RegisterValueChangedCallback(evt =>
            {
                _isEditMode = evt.newValue;
                _graphView?.SetEditMode(_isEditMode);
            });
            toolbar.Add(editToggle);

            // Negative lines toggle
            var negativeToggle = new ToolbarToggle()
            {
                text = "Negative",
                value = false
            };
            negativeToggle.tooltip = "ネガティブタグ(!付き)の\n抑制ラインを表示";
            negativeToggle.RegisterValueChangedCallback(evt =>
            {
                _graphView?.SetShowNegativeEdges(evt.newValue);
            });
            toolbar.Add(negativeToggle);

            root.Add(toolbar);

            // Graph View
            _graphView = new EventGraphView();
            _graphView.AddToClassList("event-graph-view");

            root.Add(_graphView);

            if (_eventDataList != null && _eventDataList.Count > 0)
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds, _itemNameMap);
                _graphView.SetEditMode(_isEditMode);
            }
        }

        private void LoadData()
        {
            // Load EventData assets from folder
            _eventDataList = new List<EventData>();
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EVENTDATA_DIR_PATH });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (asset != null && !asset.IsDeleted) _eventDataList.Add(asset);
            }
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
