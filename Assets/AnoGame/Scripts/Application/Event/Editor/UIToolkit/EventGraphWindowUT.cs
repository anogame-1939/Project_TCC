using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    public class EventGraphWindowUT : EditorWindow
    {
        private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_batch.json";

        private EventGraphView _graphView;
        private EventList _data;

        [MenuItem("AnoGame/Event Graph")]
        public static void Open()
        {
            var window = GetWindow<EventGraphWindowUT>("Event Graph (UT)");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            LoadData(); // Load data first so we have it for BuildUI
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

            // Graph View
            _graphView = new EventGraphView();
            _graphView.AddToClassList("event-graph-view");
            _graphView.OnGraphDataChanged = () =>
            {
                // Determine if we should autosave or just mark dirty
                // Since it's JSON file based, we might want manual save or auto-save on change.
                // For now let's just keep track in memory, user must hit Save.
                // But we can add a dirty flag to window?
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
                _data = new EventList { events = new System.Collections.Generic.List<EventJsonItem>() };
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(JSON_PATH);
                // The original code wrapped it: "{\"events\":" + jsonContent + "}";
                // Let's check if the file is a bare array or object.
                // Original code implies the file content is JUST the array: [ { ... }, { ... } ]
                // So wrapping is needed if we use JsonUtility with EventList wrapper.

                string wrappedJson = "{\"events\":" + jsonContent + "}";
                _data = JsonUtility.FromJson<EventList>(wrappedJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load Event Graph data: {ex.Message}");
                _data = new EventList { events = new System.Collections.Generic.List<EventJsonItem>() };
            }
        }

        private void SaveData()
        {
            if (_data == null) return;

            try
            {
                // We need to extract the array part manually because JsonUtility adds the wrapper.
                // Or we can just use a library like Newtonsoft if available, but let's stick to Unity if possible.
                // Re-serializing:
                string json = JsonUtility.ToJson(_data, true);

                // JsonUtility.ToJson(_data) produces:
                // {
                //     "events": [ ... ]
                // }

                // The original file format expected simply: [ ... ]
                // So we need to strip the outer object.
                // However, doing string manipulation on JSON is risky.
                // But since we control the wrapper, we know the structure.

                // Quick hack:
                // Find first '[' and last ']'
                int start = json.IndexOf('[');
                int end = json.LastIndexOf(']');

                if (start >= 0 && end >= 0)
                {
                    string arrayJson = json.Substring(start, end - start + 1);
                    File.WriteAllText(JSON_PATH, arrayJson);
                    AssetDatabase.Refresh();
                    Debug.Log("Event Graph saved.");
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
