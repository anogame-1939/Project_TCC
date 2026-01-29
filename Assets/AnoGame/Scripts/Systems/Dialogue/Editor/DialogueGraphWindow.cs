using UnityEngine;
using UnityEditor;

namespace AnoGame.Systems.Dialogue.Editor
{
    // Main Window for Dialogue Graph
    public class DialogueGraphWindow : EditorWindow
    {
        private MasterDialogueData _data;
        private DialogueGraphCanvas _canvas;
        private DialogueGraphSidebar _sidebar;

        [MenuItem("AnoGame/Dialogue System/Open Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueGraphWindow>("Dialogue Graph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            if (_data == null)
            {
                FindData();
            }
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
                    _canvas.ScrollPos = pos - new Vector2(100, 100);
                    Repaint();
                };

                _sidebar.OnSelectSection = (chapter, section) =>
                {
                    _canvas.SetFilter(chapter, section);
                    Repaint();
                };
            }
        }

        private bool _showSidebar = true;

        private void OnGUI()
        {
            if (_data == null)
            {
                EditorGUILayout.HelpBox("Master Dialogue Data not found or assigned.", MessageType.Warning);
                _data = (MasterDialogueData)EditorGUILayout.ObjectField(_data, typeof(MasterDialogueData), false);
                if (_data != null) InitializeSubSystems();
                return;
            }

            if (_canvas == null || _sidebar == null) InitializeSubSystems();

            // 1. Draw Toolbar with GUILayout
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _showSidebar = GUILayout.Toggle(_showSidebar, "Sidebar", EditorStyles.toolbarButton);

                if (GUILayout.Button("Show All", EditorStyles.toolbarButton))
                {
                    _canvas.SetFilter(null, null);
                }

                GUILayout.FlexibleSpace();
            }

            // 2. Calculate Rects manually
            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            Rect mainArea = new Rect(0, toolbarHeight, position.width, position.height - toolbarHeight);

            // 3. Draw Canvas (Full Main Area)
            // We strip any GUILayout logic from relying on "rest of window"
            _canvas.Draw(mainArea, _showSidebar ? 250f : 0f);

            // 4. Draw Sidebar Overlay
            if (_showSidebar)
            {
                Rect sidebarRect = new Rect(mainArea.x, mainArea.y, 250f, mainArea.height);

                // Background & Border
                EditorGUI.DrawRect(sidebarRect, new Color(0.2f, 0.2f, 0.2f, 1f));
                EditorGUI.DrawRect(new Rect(sidebarRect.xMax - 1, sidebarRect.y, 1, sidebarRect.height), Color.black);

                // Important: BeginArea resets coordinate system to (0,0) relative to rect
                GUILayout.BeginArea(sidebarRect);
                _sidebar.Draw(250f);
                GUILayout.EndArea();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_data);
            }
        }
    }
}
