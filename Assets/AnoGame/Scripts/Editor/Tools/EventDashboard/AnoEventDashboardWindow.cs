#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using AnoGame.Application.Event;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Editor.Tools
{
    public class AnoEventDashboardWindow : EditorWindow
    {
        private ListView _eventListView;
        private ScrollView _detailView;
        private List<AnoEventRoot> _eventRoots = new List<AnoEventRoot>();

        [MenuItem("AnoGame/Tools/Event Dashboard")]
        public static void ShowWindow()
        {
            AnoEventDashboardWindow wnd = GetWindow<AnoEventDashboardWindow>();
            wnd.titleContent = new GUIContent("Event Dashboard");
        }

        public void CreateGUI()
        {
            // Setup Two-Pane Layout
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            // Left Pane: List View
            var leftPane = new VisualElement();
            leftPane.style.flexGrow = 1;
            splitView.Add(leftPane);

            var header = new Label("Scene Events");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 5;
            header.style.paddingTop = 5;
            header.style.paddingBottom = 5;
            header.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            leftPane.Add(header);

            var refreshBtn = new Button(RefreshList) { text = "↻ Refresh (Scan Scene)" };
            leftPane.Add(refreshBtn);

            _eventListView = new ListView();
            _eventListView.style.flexGrow = 1;
            _eventListView.makeItem = () => new Label();
            _eventListView.bindItem = (element, i) =>
            {
                var label = (Label)element;
                var root = _eventRoots[i];
                if (root != null)
                {
                    string eventId = root.EventData != null ? root.EventData.EventId : "???";
                    label.text = $"[{eventId}] {root.gameObject.name}";
                }
                else
                {
                    label.text = "(Destroyed)";
                }
            };
            _eventListView.selectionChanged += OnSelectionChanged;
            leftPane.Add(_eventListView);

            // Right Pane: Detail View
            _detailView = new ScrollView();
            _detailView.style.flexGrow = 1;
            _detailView.style.paddingLeft = 10;
            _detailView.style.paddingRight = 10;
            _detailView.style.paddingTop = 10;
            splitView.Add(_detailView);

            RefreshList();
        }

        private void OnEnable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            _eventRoots.Clear();
            _eventRoots.AddRange(Object.FindObjectsByType<AnoEventRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            // Sort by EventID if possible, otherwise by name
            _eventRoots = _eventRoots.OrderBy(r => 
            {
                if (r != null && r.EventData != null) return r.EventData.EventId;
                if (r != null) return r.gameObject.name;
                return "";
            }).ToList();

            if (_eventListView != null)
            {
                _eventListView.itemsSource = _eventRoots;
                _eventListView.RefreshItems();
            }

            ShowDefaultDetail();
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            _detailView.Clear();

            var selectedRoot = selection.FirstOrDefault() as AnoEventRoot;
            if (selectedRoot == null)
            {
                ShowDefaultDetail();
                return;
            }

            BuildDetailView(selectedRoot);
        }

        private void ShowDefaultDetail()
        {
            _detailView.Clear();
            var label = new Label("Select an event from the list to view details.");
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 20;
            label.style.color = Color.gray;
            _detailView.Add(label);
        }

        private void BuildDetailView(AnoEventRoot root)
        {
            // Title Header
            var titleLabel = new Label(root.gameObject.name);
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            _detailView.Add(titleLabel);

            // EventData Info Group
            var infoBox = new Box();
            infoBox.style.paddingBottom = 5;
            infoBox.style.paddingTop = 5;
            infoBox.style.paddingLeft = 5;
            infoBox.style.paddingRight = 5;
            infoBox.style.marginBottom = 15;
            _detailView.Add(infoBox);

            if (root.EventData != null)
            {
                infoBox.Add(new Label($"Event ID: {root.EventData.EventId}"));
                infoBox.Add(new Label($"Category: {root.EventData.Category}"));
                
                var descLabel = new Label($"Description:\n{root.EventData.Description}");
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                descLabel.style.marginTop = 5;
                infoBox.Add(descLabel);

                var pingDataBtn = new Button(() => { EditorGUIUtility.PingObject(root.EventData); }) { text = "■ Ping EventData Asset" };
                pingDataBtn.style.marginTop = 5;
                infoBox.Add(pingDataBtn);
            }
            else
            {
                var warning = new Label("! EventData is not assigned.");
                warning.style.color = Color.yellow;
                infoBox.Add(warning);
            }

            // Controls Header
            var controlsHeader = new Label("Quick Access Controls");
            controlsHeader.style.fontSize = 14;
            controlsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            controlsHeader.style.marginBottom = 5;
            _detailView.Add(controlsHeader);

            // Open Timeline Action (The most important action)
            var timelineBtn = new Button(() => 
            {
                if (root.Trigger != null)
                {
                    Selection.activeGameObject = root.Trigger;
                    EditorGUIUtility.PingObject(root.Trigger);
                    EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
                }
                else
                {
                    Debug.LogWarning("[Event Dashboard] Trigger is missing on the selected EventRoot.");
                }
            }) 
            { 
                text = "▶ Open Timeline" 
            };
            timelineBtn.style.height = 40;
            timelineBtn.style.fontSize = 14;
            timelineBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            timelineBtn.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f, 1f); // Dark Green Custom Button
            timelineBtn.style.marginBottom = 10;
            _detailView.Add(timelineBtn);

            // Hierarchy Selectors
            var rootBtn = new Button(() => 
            { 
                Selection.activeGameObject = root.gameObject;
                EditorGUIUtility.PingObject(root.gameObject);
            }) 
            { text = "■ Select System Root" };
            rootBtn.style.marginBottom = 5;
            _detailView.Add(rootBtn);

            var triggerBtn = new Button(() => 
            { 
                if (root.Trigger != null) 
                {
                    Selection.activeGameObject = root.Trigger;
                    EditorGUIUtility.PingObject(root.Trigger);
                } 
            }) 
            { text = "⚙ Select Trigger" };
            triggerBtn.style.marginBottom = 5;
            _detailView.Add(triggerBtn);

            var receptorBtn = new Button(() => 
            { 
                if (root.Receptor != null) 
                {
                    Selection.activeGameObject = root.Receptor;
                    EditorGUIUtility.PingObject(root.Receptor);
                } 
            }) 
            { text = "🔍 Select Receptor" };
            receptorBtn.style.marginBottom = 15;
            _detailView.Add(receptorBtn);

            // Target Inspection
            if (root.Trigger != null)
            {
                var inspLabel = new Label("Trigger Inspector Preview");
                inspLabel.style.fontSize = 12;
                inspLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                inspLabel.style.marginTop = 10;
                inspLabel.style.marginBottom = 5;
                _detailView.Add(inspLabel);

                var editor = UnityEditor.Editor.CreateEditor(root.Trigger);
                var inspectorElement = new InspectorElement(editor);
                inspectorElement.style.borderTopWidth = 1;
                inspectorElement.style.borderTopColor = Color.gray;
                inspectorElement.style.paddingTop = 5;
                _detailView.Add(inspectorElement);
            }
        }
    }
}
#endif
