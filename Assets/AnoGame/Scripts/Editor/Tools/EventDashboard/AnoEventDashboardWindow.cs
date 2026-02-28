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
        private bool _timelineMode = true;

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
            leftPane.style.minWidth = 200;
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

            // Timeline Mode Toggle
            var tlToggle = new Toggle();
            tlToggle.SetValueWithoutNotify(_timelineMode);
            tlToggle.tooltip = "ON: Timeline直アクセス / OFF: Root選択のみ";
            tlToggle.RegisterValueChangedCallback(evt => _timelineMode = evt.newValue);
            tlToggle.style.marginLeft = 4;
            toolbar.Add(tlToggle);

            var tlLabel = new Label("TL");
            tlLabel.style.fontSize = 10;
            tlLabel.tooltip = "ON: Timeline直アクセス / OFF: Root選択のみ";
            toolbar.Add(tlLabel);

            _eventListView = new ListView();
            _eventListView.style.flexGrow = 1;
            _eventListView.makeItem = () =>
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.alignItems = Align.Center;

                var colorDot = new VisualElement();
                colorDot.name = "tag-dot";
                colorDot.style.width = 8;
                colorDot.style.height = 8;
                colorDot.style.flexShrink = 0;
                colorDot.style.borderTopLeftRadius = 4;
                colorDot.style.borderTopRightRadius = 4;
                colorDot.style.borderBottomLeftRadius = 4;
                colorDot.style.borderBottomRightRadius = 4;
                colorDot.style.marginLeft = 4;
                colorDot.style.marginRight = 4;
                container.Add(colorDot);

                var lbl = new Label();
                lbl.name = "event-label";
                lbl.style.paddingLeft = 4;
                container.Add(lbl);

                return container;
            };
            _eventListView.bindItem = (element, i) =>
            {
                var container = (VisualElement)element;
                var colorDot = container.Q<VisualElement>("tag-dot");
                var label = container.Q<Label>("event-label");
                var root = _eventRoots[i];
                if (root != null)
                {
                    label.text = FormatEventDisplayName(root);
                    Color tagColor = AnoEventRoot.GetTagColor(root.ColorTag);
                    colorDot.style.backgroundColor = root.ColorTag != Application.Event.EventColorTag.None
                        ? new StyleColor(tagColor)
                        : new StyleColor(Color.clear);
                    colorDot.style.display = root.ColorTag != Application.Event.EventColorTag.None
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
                else
                {
                    label.text = "(Destroyed)";
                    colorDot.style.display = DisplayStyle.None;
                }
            };
            _eventListView.selectionChanged += OnSelectionChanged;

            // ダブルクリックでシーンカメラを移動（ズームなし）
            _eventListView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    var selectedRoot = _eventListView.selectedItem as AnoEventRoot;
                    if (selectedRoot != null)
                    {
                        Selection.activeGameObject = selectedRoot.gameObject;
                        var sceneView = SceneView.lastActiveSceneView;
                        if (sceneView != null)
                        {
                            sceneView.pivot = selectedRoot.transform.position;
                            sceneView.Repaint();
                        }
                    }
                }
            });
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
            Selection.selectionChanged += OnUnitySelectionChanged;
        }

        private void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            Selection.selectionChanged -= OnUnitySelectionChanged;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            RefreshList();
        }

        /// <summary>
        /// Unityのグローバル選択変更を監視し、AnoEventRootを持つオブジェクトが選択されたらリストで自動選択する。
        /// シーン上のVisualizer等との連携を相互参照なしで実現。
        /// </summary>
        private void OnUnitySelectionChanged()
        {
            if (_eventListView == null || _eventRoots == null) return;

            var activeGo = Selection.activeGameObject;
            if (activeGo == null) return;

            var eventRoot = activeGo.GetComponent<AnoEventRoot>();
            if (eventRoot == null)
                eventRoot = activeGo.GetComponentInParent<AnoEventRoot>();
            if (eventRoot == null) return;

            int index = _eventRoots.IndexOf(eventRoot);
            if (index >= 0 && _eventListView.selectedIndex != index)
            {
                _eventListView.SetSelection(index);
                _eventListView.ScrollToItem(index);
            }
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

            // リスト選択時の動作
            if (_timelineMode)
            {
                // Timeline直アクセスモード: Triggerを選択してTimelineを開く
                if (selectedRoot.Trigger != null)
                {
                    Selection.activeGameObject = selectedRoot.Trigger;
                    EditorGUIUtility.PingObject(selectedRoot.Trigger);
                    EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
                }
            }
            else
            {
                // Root選択のみモード
                Selection.activeGameObject = selectedRoot.gameObject;
                EditorGUIUtility.PingObject(selectedRoot.gameObject);
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
            _detailView.Clear();

            // Title Header
            var titleLabel = new Label(root.gameObject.name);
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            _detailView.Add(titleLabel);

            // ── カラータグ選択 ──
            var tagHeader = new Label("Color Tag");
            tagHeader.style.fontSize = 12;
            tagHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            tagHeader.style.marginBottom = 4;
            _detailView.Add(tagHeader);

            var tagRow = new VisualElement();
            tagRow.style.flexDirection = FlexDirection.Row;
            tagRow.style.marginBottom = 10;
            tagRow.style.flexWrap = Wrap.Wrap;
            _detailView.Add(tagRow);

            var allTags = (Application.Event.EventColorTag[])System.Enum.GetValues(
                typeof(Application.Event.EventColorTag));

            foreach (var tag in allTags)
            {
                var tagBtn = new Button();
                tagBtn.style.width = 24;
                tagBtn.style.height = 24;
                tagBtn.style.marginRight = 4;
                tagBtn.style.marginBottom = 4;
                tagBtn.style.borderTopLeftRadius = 12;
                tagBtn.style.borderTopRightRadius = 12;
                tagBtn.style.borderBottomLeftRadius = 12;
                tagBtn.style.borderBottomRightRadius = 12;

                if (tag == Application.Event.EventColorTag.None)
                {
                    tagBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                    tagBtn.text = "\u00d7";
                    tagBtn.style.fontSize = 10;
                    tagBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                    tagBtn.style.color = Color.white;
                    tagBtn.style.paddingLeft = 0;
                    tagBtn.style.paddingRight = 0;
                    tagBtn.style.paddingTop = 0;
                    tagBtn.style.paddingBottom = 0;
                }
                else
                {
                    tagBtn.style.backgroundColor = AnoEventRoot.GetTagColor(tag);
                }

                // 選択中のタグをハイライト
                if (root.ColorTag == tag)
                {
                    tagBtn.style.borderTopWidth = 2;
                    tagBtn.style.borderBottomWidth = 2;
                    tagBtn.style.borderLeftWidth = 2;
                    tagBtn.style.borderRightWidth = 2;
                    tagBtn.style.borderTopColor = Color.white;
                    tagBtn.style.borderBottomColor = Color.white;
                    tagBtn.style.borderLeftColor = Color.white;
                    tagBtn.style.borderRightColor = Color.white;
                }

                var capturedTag = tag;
                tagBtn.clicked += () =>
                {
                    Undo.RecordObject(root, "Change EventRoot Color Tag");
                    root.ColorTag = capturedTag;
                    EditorUtility.SetDirty(root);
                    SceneView.RepaintAll();
                    // UIを再構築
                    BuildDetailView(root);
                    _eventListView.RefreshItems();
                };

                tagRow.Add(tagBtn);
            }

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
            { text = "-- Select Trigger" };
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
            { text = "-- Select Receptor" };
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
