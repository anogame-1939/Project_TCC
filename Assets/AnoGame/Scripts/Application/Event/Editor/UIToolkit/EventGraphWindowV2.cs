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

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventGraphWindowV2 : EditorWindow
    {
        private const string EVENTDATA_DIR_PATH = "Assets/AnoGame/Data/Events/Story2";
        private const string ITEMS_JSON_PATH = "Assets/AnoGame/Data/ItemsResources/items_batch.json";

        private EventGraphViewV2 _graphView;
        private List<EventData> _eventDataList;
        private HashSet<string> _knownItemIds;

        [MenuItem("AnoGame/Event Graph V2")]
        public static void Open()
        {
            var window = GetWindow<EventGraphWindowV2>("Event Graph V2");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            LoadData();
            BuildUI();
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                _graphView.OnGraphDataChanged = null;
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
                _graphView?.PopulateGraph(_eventDataList, _knownItemIds);
            })
            { text = "Reload" };
            toolbar.Add(reloadBtn);

            var autoLayoutBtn = new ToolbarButton(() => _graphView?.AutoLayout()) { text = "Auto Layout" };
            toolbar.Add(autoLayoutBtn);

            var savePositionsBtn = new ToolbarButton(() => _graphView?.SaveNodePositions()) { text = "Save Positions" };
            toolbar.Add(savePositionsBtn);

            root.Add(toolbar);

            // Graph View V2
            _graphView = new EventGraphViewV2();
            _graphView.AddToClassList("event-graph-view");

            root.Add(_graphView);

            if (_eventDataList != null && _eventDataList.Count > 0)
            {
                _graphView.PopulateGraph(_eventDataList, _knownItemIds);
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
                if (asset != null) _eventDataList.Add(asset);
            }
            _eventDataList.Sort((a, b) => string.Compare(a.EventId, b.EventId, StringComparison.Ordinal));

            // Load known item IDs from items_batch.json
            _knownItemIds = new HashSet<string>();
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
                                _knownItemIds.Add(item.id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load items JSON: {ex.Message}");
                }
            }

            Debug.Log($"Event Graph V2: Loaded {_eventDataList.Count} EventData assets, {_knownItemIds.Count} known items.");
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
        }
    }
}
#endif
