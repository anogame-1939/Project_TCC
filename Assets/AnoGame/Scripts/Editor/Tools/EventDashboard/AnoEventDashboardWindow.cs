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
        private readonly HashSet<Application.Event.EventColorTag> _activeTagFilters = new HashSet<Application.Event.EventColorTag>();
        private bool _allTagsActive = true;
        private bool _suppressUnitySelectionSync;

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

            // ── タグフィルタバー ──
            var tagFilterBar = new VisualElement();
            tagFilterBar.style.flexDirection = FlexDirection.Row;
            tagFilterBar.style.alignItems = Align.Center;
            tagFilterBar.style.paddingLeft = 4;
            tagFilterBar.style.paddingRight = 4;
            tagFilterBar.style.paddingTop = 3;
            tagFilterBar.style.paddingBottom = 3;
            tagFilterBar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            tagFilterBar.style.borderBottomWidth = 1;
            tagFilterBar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            leftPane.Add(tagFilterBar);

            var filterLabel = new Label("Tag:");
            filterLabel.style.fontSize = 10;
            filterLabel.style.marginRight = 4;
            filterLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            tagFilterBar.Add(filterLabel);

            BuildTagFilterButtons(tagFilterBar);

            // Toolbar: Refresh + Search + TL Toggle
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
            _eventListView.selectionType = SelectionType.Multiple;
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

            // Ctrl+A 全選択
            _eventListView.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.A)
                {
                    var allIndices = new List<int>();
                    for (int i = 0; i < _eventRoots.Count; i++)
                        allIndices.Add(i);
                    _eventListView.SetSelection(allIndices);
                    evt.StopPropagation();
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
            AnoEventRootVisualizer.OnSceneSelectionChanged += SyncListFromSceneSelection;
        }

        private void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            Selection.selectionChanged -= OnUnitySelectionChanged;
            AnoEventRootVisualizer.OnSceneSelectionChanged -= SyncListFromSceneSelection;
        }

        /// <summary>
        /// シーン上の複数選択をDashboardのListViewに同期する。
        /// </summary>
        private void SyncListFromSceneSelection()
        {
            if (_eventListView == null) return;

            _suppressUnitySelectionSync = true;

            var indices = new List<int>();
            for (int i = 0; i < _eventRoots.Count; i++)
            {
                if (AnoEventRootVisualizer.SelectedRoots.Contains(_eventRoots[i]))
                    indices.Add(i);
            }
            _eventListView.SetSelection(indices);

            EditorApplication.delayCall += () => _suppressUnitySelectionSync = false;
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
            if (_suppressUnitySelectionSync) return;
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

            _eventRoots = _allEventRoots.Where(r =>
            {
                if (r == null) return false;

                // タグフィルタ
                if (!_allTagsActive && !_activeTagFilters.Contains(r.ColorTag))
                    return false;

                // テキストフィルタ
                if (!string.IsNullOrEmpty(filter))
                {
                    string f = filter.ToLowerInvariant();
                    string displayName = FormatEventDisplayName(r).ToLowerInvariant();
                    string fullName = r.gameObject.name.ToLowerInvariant();
                    if (!displayName.Contains(f) && !fullName.Contains(f))
                        return false;
                }

                return true;
            }).ToList();

            // Visualizerのギズモ表示も同期
            AnoEventRootVisualizer.HiddenTags.Clear();
            if (!_allTagsActive)
            {
                var allTags = (Application.Event.EventColorTag[])System.Enum.GetValues(
                    typeof(Application.Event.EventColorTag));
                foreach (var tag in allTags)
                {
                    if (!_activeTagFilters.Contains(tag))
                        AnoEventRootVisualizer.HiddenTags.Add(tag);
                }
            }
            SceneView.RepaintAll();

            if (_eventListView != null)
            {
                _eventListView.itemsSource = _eventRoots;
                _eventListView.RefreshItems();
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            _detailView.Clear();

            var selectedRoots = selection.OfType<AnoEventRoot>().ToList();

            // VisualizerのSelectedRootsを同期
            // シーンからの同期トリガー時はSelectedRootsを上書きしない
            // (SyncListFromSceneSelection → SetSelection → OnSelectionChanged の連鎖で
            //  シーン側で既に設定済みのSelectedRootsがクリアされるのを防ぐ)
            if (!_suppressUnitySelectionSync)
            {
                AnoEventRootVisualizer.SelectedRoots.Clear();
                foreach (var root in selectedRoots)
                    AnoEventRootVisualizer.SelectedRoots.Add(root);
            }
            SceneView.RepaintAll();

            Debug.Log($"[Dashboard] OnSelectionChanged: {selectedRoots.Count} items selected, SelectedRoots={AnoEventRootVisualizer.SelectedRoots.Count}");

            if (selectedRoots.Count == 0)
            {
                ShowDefaultDetail();
                return;
            }

            if (selectedRoots.Count == 1)
            {
                BuildDetailView(selectedRoots[0]);

                // リスト選択時の動作
                if (_timelineMode)
                {
                    if (selectedRoots[0].Trigger != null)
                    {
                        Selection.activeGameObject = selectedRoots[0].Trigger;
                        EditorGUIUtility.PingObject(selectedRoots[0].Trigger);
                        EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
                    }
                }
                else
                {
                    Selection.activeGameObject = selectedRoots[0].gameObject;
                    EditorGUIUtility.PingObject(selectedRoots[0].gameObject);
                }
            }
            else
            {
                // 複数選択時: 一括操作ビュー
                BuildMultiSelectionView(selectedRoots);
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

        private VisualElement _tagFilterContainer;

        private void BuildTagFilterButtons(VisualElement parent)
        {
            _tagFilterContainer = parent;

            // ALLボタン
            var allBtn = new Button(() =>
            {
                _allTagsActive = true;
                _activeTagFilters.Clear();
                ApplyFilter();
                RebuildTagFilterVisuals();
            })
            { text = "ALL" };
            allBtn.style.height = 18;
            allBtn.style.fontSize = 9;
            allBtn.style.paddingLeft = 4;
            allBtn.style.paddingRight = 4;
            allBtn.style.paddingTop = 0;
            allBtn.style.paddingBottom = 0;
            allBtn.style.marginRight = 4;
            allBtn.name = "tag-filter-all";
            parent.Add(allBtn);

            var allTags = (Application.Event.EventColorTag[])System.Enum.GetValues(
                typeof(Application.Event.EventColorTag));

            foreach (var tag in allTags)
            {
                var btn = new Button();
                btn.style.width = 16;
                btn.style.height = 16;
                btn.style.marginRight = 2;
                btn.style.borderTopLeftRadius = 8;
                btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8;
                btn.style.borderBottomRightRadius = 8;
                btn.style.paddingLeft = 0;
                btn.style.paddingRight = 0;
                btn.style.paddingTop = 0;
                btn.style.paddingBottom = 0;

                if (tag == Application.Event.EventColorTag.None)
                {
                    btn.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                    btn.text = "\u00d7";
                    btn.style.fontSize = 8;
                    btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                    btn.style.color = Color.white;
                }
                else
                {
                    btn.style.backgroundColor = AnoEventRoot.GetTagColor(tag);
                }

                btn.name = $"tag-filter-{tag}";

                var capturedTag = tag;
                btn.clicked += () =>
                {
                    if (_allTagsActive)
                    {
                        // ALL -> 個別モードへ切替、このタグだけON
                        _allTagsActive = false;
                        _activeTagFilters.Clear();
                        _activeTagFilters.Add(capturedTag);
                    }
                    else if (_activeTagFilters.Contains(capturedTag))
                    {
                        _activeTagFilters.Remove(capturedTag);
                        if (_activeTagFilters.Count == 0)
                            _allTagsActive = true;
                    }
                    else
                    {
                        _activeTagFilters.Add(capturedTag);
                        // 全タグが有効になったらALLに戻す
                        if (_activeTagFilters.Count == allTags.Length)
                            _allTagsActive = true;
                    }
                    ApplyFilter();
                    RebuildTagFilterVisuals();
                };

                parent.Add(btn);
            }

            RebuildTagFilterVisuals();
        }

        private void RebuildTagFilterVisuals()
        {
            if (_tagFilterContainer == null) return;

            var allTags = (Application.Event.EventColorTag[])System.Enum.GetValues(
                typeof(Application.Event.EventColorTag));

            // ALLボタンのスタイル
            var allBtn = _tagFilterContainer.Q<Button>("tag-filter-all");
            if (allBtn != null)
            {
                allBtn.style.backgroundColor = _allTagsActive
                    ? new Color(0.3f, 0.5f, 0.8f, 1f)
                    : new Color(0.25f, 0.25f, 0.25f, 1f);
            }

            foreach (var tag in allTags)
            {
                var btn = _tagFilterContainer.Q<Button>($"tag-filter-{tag}");
                if (btn == null) continue;

                bool isActive = _allTagsActive || _activeTagFilters.Contains(tag);
                btn.style.opacity = isActive ? 1f : 0.3f;

                if (!_allTagsActive && _activeTagFilters.Contains(tag))
                {
                    btn.style.borderTopWidth = 2;
                    btn.style.borderBottomWidth = 2;
                    btn.style.borderLeftWidth = 2;
                    btn.style.borderRightWidth = 2;
                    btn.style.borderTopColor = Color.white;
                    btn.style.borderBottomColor = Color.white;
                    btn.style.borderLeftColor = Color.white;
                    btn.style.borderRightColor = Color.white;
                }
                else
                {
                    btn.style.borderTopWidth = 0;
                    btn.style.borderBottomWidth = 0;
                    btn.style.borderLeftWidth = 0;
                    btn.style.borderRightWidth = 0;
                }
            }
        }

        private void BuildMultiSelectionView(List<AnoEventRoot> roots)
        {
            _detailView.Clear();

            var titleLabel = new Label($"{roots.Count} items selected");
            titleLabel.style.fontSize = 16;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            _detailView.Add(titleLabel);

            // ── 一括タグ設定 ──
            var tagHeader = new Label("Set Tag (Bulk)");
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
                tagBtn.style.width = 28;
                tagBtn.style.height = 28;
                tagBtn.style.marginRight = 4;
                tagBtn.style.marginBottom = 4;
                tagBtn.style.borderTopLeftRadius = 14;
                tagBtn.style.borderTopRightRadius = 14;
                tagBtn.style.borderBottomLeftRadius = 14;
                tagBtn.style.borderBottomRightRadius = 14;

                if (tag == Application.Event.EventColorTag.None)
                {
                    tagBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                    tagBtn.text = "\u00d7";
                    tagBtn.style.fontSize = 12;
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

                var capturedTag = tag;
                tagBtn.clicked += () =>
                {
                    foreach (var root in roots)
                    {
                        Undo.RecordObject(root, "Bulk Change EventRoot Color Tag");
                        root.ColorTag = capturedTag;
                        EditorUtility.SetDirty(root);
                    }
                    SceneView.RepaintAll();
                    _eventListView.RefreshItems();
                    BuildMultiSelectionView(roots);
                };

                tagRow.Add(tagBtn);
            }

            // ── 選択中アイテム一覧 ──
            var listHeader = new Label("Selected Items:");
            listHeader.style.fontSize = 12;
            listHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            listHeader.style.marginTop = 10;
            listHeader.style.marginBottom = 4;
            _detailView.Add(listHeader);

            foreach (var root in roots)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2;

                var dot = new VisualElement();
                dot.style.width = 8;
                dot.style.height = 8;
                dot.style.borderTopLeftRadius = 4;
                dot.style.borderTopRightRadius = 4;
                dot.style.borderBottomLeftRadius = 4;
                dot.style.borderBottomRightRadius = 4;
                dot.style.marginRight = 4;
                dot.style.flexShrink = 0;
                dot.style.backgroundColor = root.ColorTag == Application.Event.EventColorTag.None
                    ? new Color(0.5f, 0.5f, 0.5f)
                    : (StyleColor)AnoEventRoot.GetTagColor(root.ColorTag);
                row.Add(dot);

                var nameLabel = new Label(FormatEventDisplayName(root));
                nameLabel.style.fontSize = 11;
                row.Add(nameLabel);

                _detailView.Add(row);
            }
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
