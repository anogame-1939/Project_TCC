using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.AnoNarrative.Editor
{
    /// <summary>
    /// UIToolkit-based EditorWindow for the Dialogue Graph system.
    /// Replaces the IMGUI-based DialogueGraphWindow with GraphView + UIToolkit.
    /// </summary>
    public class DialogueGraphWindowUT : EditorWindow
    {
        [SerializeField]
        private MasterDialogueData _data;
        private DialogueGraphView _graphView;
        private DialogueGraphSidebarUT _sidebar;
        private VisualElement _sidebarContainer;
        [SerializeField]
        private bool _showSidebar = true;
        [SerializeField]
        private int _lastFilterEpisode = int.MinValue;
        [SerializeField]
        private int _lastFilterChapter = int.MinValue;
        [SerializeField]
        private int _lastFilterSection = int.MinValue;
        [SerializeField]
        private string _lastFilterSectionName = null;
        [SerializeField]
        private bool _useManhattanEdges = false;

        [MenuItem("AnoGame/Dialogue System/Open Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueGraphWindowUT>("Dialogue Graph (UT)");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            // Load USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/AnoNarrative/Scripts/Editor/UIToolkit/DialogueGraphStyles.uss");

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            if (_data == null)
            {
                FindData();
            }

            BuildUI();
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                _graphView.OnGraphDataChanged = null;
            }
        }

        private void FindData()
        {
            string[] guids = AssetDatabase.FindAssets("t:MasterDialogueData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _data = AssetDatabase.LoadAssetAtPath<MasterDialogueData>(path);
            }
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            var root = new VisualElement();
            root.AddToClassList("dialogue-graph-root");
            rootVisualElement.Add(root);

            if (_data == null)
            {
                BuildNoDataUI(root);
                return;
            }

            // Toolbar
            var toolbar = BuildToolbar();
            root.Add(toolbar);

            // Content area (Sidebar + GraphView)
            var content = new VisualElement();
            content.AddToClassList("dialogue-content");
            root.Add(content);

            // Sidebar
            _sidebarContainer = new VisualElement();
            _sidebarContainer.style.display = _showSidebar ? DisplayStyle.Flex : DisplayStyle.None;

            _sidebar = new DialogueGraphSidebarUT(_data);
            _sidebar.OnSelectSection = (ep, ch, sec, name) =>
            {
                _graphView?.SetFilter(ep, ch, sec, name);
                _lastFilterEpisode = ep;
                _lastFilterChapter = ch;
                _lastFilterSection = sec;
                _lastFilterSectionName = name;
                _sidebar?.RebuildTree();
            };
            _sidebar.OnRequestPanTo = (pos) =>
            {
                // Handled by SetFilter -> FrameNode in GraphView
            };

            _sidebarContainer.Add(_sidebar);
            content.Add(_sidebarContainer);

            // GraphView
            _graphView = new DialogueGraphView(_data);
            _graphView.AddToClassList("dialogue-graph-view");
            _graphView.UseManhattanEdges = _useManhattanEdges;
            _graphView.OnGraphDataChanged = () =>
            {
                _sidebar?.RebuildTree();
            };
            content.Add(_graphView);

            // Restore previous filter or set default
            if (_lastFilterEpisode != int.MinValue)
            {
                // Restore previous filter state (e.g. after domain reload)
                _graphView.SetFilter(_lastFilterEpisode, _lastFilterChapter, _lastFilterSection, _lastFilterSectionName);
            }
            else if (_data.Conversations.Count > 0)
            {
                var first = _data.Conversations[0];
                if (first.EpisodeID != int.MinValue)
                {
                    _graphView.SetFilter(first.EpisodeID, first.ChapterID, first.SectionID);
                    _lastFilterEpisode = first.EpisodeID;
                    _lastFilterChapter = first.ChapterID;
                    _lastFilterSection = first.SectionID;
                }
            }
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.AddToClassList("dialogue-toolbar");

            // Sidebar toggle
            var sidebarToggle = new ToolbarToggle { text = "Sidebar", value = _showSidebar };
            sidebarToggle.RegisterValueChangedCallback(evt =>
            {
                _showSidebar = evt.newValue;
                if (_sidebarContainer != null)
                {
                    _sidebarContainer.style.display = _showSidebar ? DisplayStyle.Flex : DisplayStyle.None;
                }
            });
            toolbar.Add(sidebarToggle);

            // Grid Layout
            var gridLayoutBtn = new ToolbarButton(() =>
            {
                _graphView?.AutoLayoutGrid();
            })
            { text = "Grid Layout" };
            toolbar.Add(gridLayoutBtn);

            // Flow Layout
            var flowLayoutBtn = new ToolbarButton(() =>
            {
                _graphView?.AutoLayoutFlow();
            })
            { text = "Flow Layout" };
            toolbar.Add(flowLayoutBtn);

            // Refresh
            var refreshBtn = new ToolbarButton(() =>
            {
                _graphView?.PopulateGraph();
                _sidebar?.RebuildTree();
            })
            { text = "Refresh" };
            toolbar.Add(refreshBtn);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Edge type toggle (right side)
            var edgeToggle = new ToolbarToggle { text = "Manhattan Edge", value = _useManhattanEdges };
            edgeToggle.RegisterValueChangedCallback(evt =>
            {
                _useManhattanEdges = evt.newValue;
                if (_graphView != null)
                {
                    _graphView.UseManhattanEdges = evt.newValue;
                    _graphView.PopulateGraph();
                }
            });
            toolbar.Add(edgeToggle);

            return toolbar;
        }

        private void BuildNoDataUI(VisualElement root)
        {
            var container = new VisualElement();
            container.AddToClassList("no-data-container");

            var label = new Label("Master Dialogue Data not found or assigned.");
            label.AddToClassList("no-data-label");
            container.Add(label);

            var objField = new ObjectField("Data Asset")
            {
                objectType = typeof(MasterDialogueData),
                value = _data
            };
            objField.RegisterValueChangedCallback(evt =>
            {
                _data = evt.newValue as MasterDialogueData;
                if (_data != null)
                {
                    BuildUI();
                }
            });
            container.Add(objField);

            root.Add(container);
        }
    }
}
