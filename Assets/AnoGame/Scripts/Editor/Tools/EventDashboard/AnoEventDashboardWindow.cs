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
        private ToolbarSearchField _searchField;
        private List<AnoEventRoot> _allEventRoots = new List<AnoEventRoot>();
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
            leftPane.style.overflow = Overflow.Hidden;
            splitView.Add(leftPane);

            var header = new Label("Scene Events");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 5;
            header.style.paddingTop = 5;
            header.style.paddingBottom = 5;
            header.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            leftPane.Add(header);

            // Toolbar: Refresh + Search
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;
            toolbar.style.paddingTop = 2;
            toolbar.style.paddingBottom = 2;
            leftPane.Add(toolbar);

            var refreshBtn = new Button(RefreshList) { text = "↻" };
            refreshBtn.style.width = 24;
            refreshBtn.style.height = 24;
            refreshBtn.style.fontSize = 14;
            refreshBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            refreshBtn.style.paddingLeft = 0;
            refreshBtn.style.paddingRight = 0;
            refreshBtn.style.paddingTop = 0;
            refreshBtn.style.paddingBottom = 0;
            refreshBtn.style.marginRight = 4;
            toolbar.Add(refreshBtn);

            _searchField = new ToolbarSearchField();
            _searchField.style.flexGrow = 1;
            _searchField.style.flexShrink = 1;
            _searchField.style.minWidth = 0;
            _searchField.RegisterValueChangedCallback(evt => ApplyFilter());
            toolbar.Add(_searchField);

            _eventListView = new ListView();
            _eventListView.style.flexGrow = 1;
            _eventListView.makeItem = () =>
            {
                var lbl = new Label();
                lbl.style.paddingLeft = 8;
                return lbl;
            };
            _eventListView.bindItem = (element, i) =>
            {
                var label = (Label)element;
                var root = _eventRoots[i];
                if (root != null)
                {
                    label.text = FormatEventDisplayName(root);
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
            _detailView.style.borderLeftWidth = 1;
            _detailView.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f, 1f);
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
            _allEventRoots.Clear();
            _allEventRoots.AddRange(Object.FindObjectsByType<AnoEventRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            // Sort by EventID if possible, otherwise by name
            _allEventRoots = _allEventRoots.OrderBy(r => 
            {
                if (r != null && r.EventData != null) return r.EventData.EventId;
                if (r != null) return r.gameObject.name;
                return "";
            }).ToList();

            ApplyFilter();
            ShowDefaultDetail();
        }

        private void ApplyFilter()
        {
            string filter = _searchField != null ? _searchField.value : "";

            if (string.IsNullOrEmpty(filter))
            {
                _eventRoots = new List<AnoEventRoot>(_allEventRoots);
            }
            else
            {
                filter = filter.ToLowerInvariant();
                _eventRoots = _allEventRoots.Where(r =>
                {
                    if (r == null) return false;
                    string displayName = FormatEventDisplayName(r).ToLowerInvariant();
                    string fullName = r.gameObject.name.ToLowerInvariant();
                    return displayName.Contains(filter) || fullName.Contains(filter);
                }).ToList();
            }

            if (_eventListView != null)
            {
                _eventListView.itemsSource = _eventRoots;
                _eventListView.RefreshItems();
            }
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

            // リスト選択時にタイムラインを自動表示
            if (selectedRoot.Trigger != null)
            {
                Selection.activeGameObject = selectedRoot.Trigger;
                EditorGUIUtility.PingObject(selectedRoot.Trigger);
                EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            }
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

        }

        /// <summary>
        /// イベント表示名のフォーマット: [EventId]_日本語ラベル
        /// 日本語が見つからない場合はオブジェクト名をそのまま使用
        /// </summary>
        private static string FormatEventDisplayName(AnoEventRoot root)
        {
            string eventId = root.EventData != null ? root.EventData.EventId : "???";
            string jaLabel = ExtractJapaneseLabel(root.gameObject.name);
            if (!string.IsNullOrEmpty(jaLabel))
                return $"{jaLabel} [{eventId}]";
            return $"[{eventId}] {root.gameObject.name}";
        }

        /// <summary>
        /// 名前の最後の_区切りセグメントを取得する
        /// 例: "Get_KeyControlPanel_制御盤の鍵入手" → "制御盤の鍵入手"
        /// </summary>
        private static string ExtractJapaneseLabel(string name)
        {
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore < 0 || lastUnderscore >= name.Length - 1)
                return name;

            return name.Substring(lastUnderscore + 1);
        }
    }
}
#endif
