using UnityEngine;
using UnityEditor;
using System.Linq;
using AnoGame.AnoNarrative;

namespace AnoGame.AnoNarrative.Editor
{
    public class DialogueGraphWindow : EditorWindow
    {
        private MasterDialogueData _data;
        private DialogueGraphCanvas _canvas;
        private DialogueGraphSidebar _sidebar;

        private bool _showSidebar = true;
        private const float SidebarWidth = 250f;

        [MenuItem("AnoGame/Dialogue Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphWindow>("Dialogue Graph");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            FindData();
            InitializeSubSystems();
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

        private void InitializeSubSystems()
        {
            if (_data != null)
            {
                _canvas = new DialogueGraphCanvas(_data, this);
                _sidebar = new DialogueGraphSidebar(_data);

                // Link events
                _sidebar.OnRequestPanTo = (pos) =>
                {
                    float offsetX = (_showSidebar ? SidebarWidth : 0f) + 50f;
                    _canvas.ScrollPos = pos - new Vector2(offsetX, 50); // Align to Top-Left with margin
                    Repaint();
                };

                _sidebar.OnSelectSection = (ep, chapter, section) =>
                {
                    // ep, chapter, section are already INTs from Sidebar
                    _canvas.SetFilter(ep, chapter, section);

                    // Auto-pan to first node of this section
                    var firstNode = _data.Conversations
                        .Where(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section)
                        .OrderBy(u => u.NodeNumber)
                        .FirstOrDefault();

                    if (firstNode != null)
                    {
                        float offsetX = (_showSidebar ? SidebarWidth : 0f) + 50f;
                        _canvas.ScrollPos = firstNode.Position - new Vector2(offsetX, 50);
                    }
                    Repaint();
                };

                // Default Initialization: Show 0/0/0
                // Use default logic to set initial filter.
                // Assuming standard start at 0, 0, 0
                int startEp = 0;
                int startCh = 0;
                int startSec = 0;

                _canvas.SetFilter(startEp, startCh, startSec);

                var startNode = _data.Conversations
                    .FirstOrDefault(u => u.EpisodeID == startEp && u.ChapterID == startCh && u.SectionID == startSec);

                if (startNode != null)
                {
                    float offsetX = (_showSidebar ? SidebarWidth : 0f) + 50f;
                    _canvas.ScrollPos = startNode.Position - new Vector2(offsetX, 50);
                }
            }
        }

        private void OnGUI()
        {
            if (_data == null)
            {
                EditorGUILayout.HelpBox("No MasterDialogueData found.", MessageType.Error);
                if (GUILayout.Button("Retry"))
                {
                    FindData();
                    InitializeSubSystems();
                }
                return;
            }

            if (_canvas == null || _sidebar == null) InitializeSubSystems();

            // Toolbar
            DrawToolbar();

            // Main Area
            Rect windowRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight);

            float sidebarW = _showSidebar ? SidebarWidth : 0f;

            // 1. Draw Canvas
            Rect canvasRect = new Rect(0, windowRect.y, windowRect.width, windowRect.height);
            // If sidebar is overlay, we might want canvas to still be full width? 
            // git version had canvas likely full width or offset?
            // "Rect sidebarRect = new Rect(mainArea.x, mainArea.y, 250f, mainArea.height);"
            // "Rect canvasRect = new Rect(0, windowRect.y, windowRect.width, windowRect.height);"
            // It seems canvas was full screen, sidebar was overlay. 
            // But if we want no overlap, let's just make sure sidebar has its own area.

            _canvas.Draw(canvasRect);

            // 2. Draw Sidebar
            if (_showSidebar)
            {
                Rect sidebarRect = new Rect(0, windowRect.y, SidebarWidth, windowRect.height);
                _sidebar.Draw(sidebarRect);

                // Divider
                EditorGUI.DrawRect(new Rect(SidebarWidth, windowRect.y, 1, windowRect.height), Color.black);
            }

        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(_showSidebar ? "Sidebar On" : "Sidebar Off", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _showSidebar = !_showSidebar;
            }

            if (GUILayout.Button("Show All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _canvas.SetFilter(-1, -1, -1);
                _canvas.Zoom = 0.5f; // Zoom out to see more
            }

            if (GUILayout.Button("Grid Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                float offsetX = (_showSidebar ? SidebarWidth : 0f);
                _canvas.AutoLayoutVisibleNodes(offsetX);
            }

            if (GUILayout.Button("Flow Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                float offsetX = (_showSidebar ? SidebarWidth : 0f);
                _canvas.AutoLayoutFlow(offsetX);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_data.name}", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }
    }
}
