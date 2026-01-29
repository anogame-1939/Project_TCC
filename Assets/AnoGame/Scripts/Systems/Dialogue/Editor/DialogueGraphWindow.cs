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
            }
        }

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

            EditorGUILayout.BeginHorizontal();

            // Draw Sidebar (Fixed width)
            _sidebar.Draw(250f);

            // Draw Canvas (Rest of space)
            Rect canvasRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // We need to end the horizontal before we begin windows usually, 
            // but for custom drawing we can manage.
            // Actually, because we use GUILayout.Window in Canvas, it complicates nesting.
            // Best practice: End layout, then draw windows in a group or specific area if possible.
            // However, GUILayout.Window is top-level. 
            // We will just draw the sidebar first, then use `BeginArea` for canvas? 
            // OR just let the windows float on top of everything. 
            // Sidebar needs to be preserved from being covered.

            // Strategy: Draw Sidebar first. Then end horizontal.
            // The space reserved by GetRect is for the canvas background.

            EditorGUILayout.EndHorizontal();

            // Draw Canvas Elements
            _canvas.Draw(canvasRect); // This handles Background, Connections, Nodes.

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_data);
            }
        }
    }
}
