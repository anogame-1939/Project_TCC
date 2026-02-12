using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventGraphWindowV2 : EditorWindow
    {
        private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_batch.json";

        private EventGraphViewV2 _graphView;
        private EventList _data;

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

            // Load USS (Reusing V1 styles for now)
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

            var saveBtn = new ToolbarButton(() => SaveData()) { text = "Save" };
            toolbar.Add(saveBtn);

            var reloadBtn = new ToolbarButton(() =>
            {
                LoadData();
                _graphView?.PopulateGraph(_data);
            })
            { text = "Reload" };
            toolbar.Add(reloadBtn);

            var autoLayoutBtn = new ToolbarButton(() => _graphView?.AutoLayout()) { text = "Auto Layout" };
            toolbar.Add(autoLayoutBtn);

            root.Add(toolbar);

            // Graph View V2
            _graphView = new EventGraphViewV2();
            _graphView.AddToClassList("event-graph-view");
            _graphView.OnGraphDataChanged = () =>
            {
                this.hasUnsavedChanges = true;
            };

            root.Add(_graphView);

            if (_data != null)
            {
                _graphView.PopulateGraph(_data);
            }
        }

        private void LoadData()
        {
            if (!File.Exists(JSON_PATH))
            {
                _data = new EventList { events = new List<EventJsonItem>() };
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(JSON_PATH);
                string wrappedJson = "{\"events\":" + jsonContent + "}";
                _data = JsonUtility.FromJson<EventList>(wrappedJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load Event Graph data: {ex.Message}");
                _data = new EventList { events = new List<EventJsonItem>() };
            }
        }

        private void SaveData()
        {
            if (_data == null) return;

            try
            {
                string json = JsonUtility.ToJson(_data, true);
                int start = json.IndexOf('[');
                int end = json.LastIndexOf(']');

                if (start >= 0 && end >= 0)
                {
                    string arrayJson = json.Substring(start, end - start + 1);
                    File.WriteAllText(JSON_PATH, arrayJson);
                    AssetDatabase.Refresh();
                    Debug.Log("Event Graph V2 saved.");
                    this.hasUnsavedChanges = false;
                }
                else
                {
                    Debug.LogError("Failed to extract array from JSON.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save Event Graph data: {ex.Message}");
            }
        }
    }
}
