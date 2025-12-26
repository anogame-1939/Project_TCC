using UnityEngine;
using UnityEditor;
using AnoGame.Scripts.Editor;
using AnoGame.EditorExtensions;

namespace AnoGame.Scripts.Editor
{
    public class UtilityLauncherWindow : EditorWindow
    {
        [MenuItem("AnoGame/Utility Launcher")]
        public static void ShowWindow()
        {
            GetWindow<UtilityLauncherWindow>("Utility Launcher");
        }

        private void OnGUI()
        {
            GUILayout.Label("AnoGame Utilities", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Object Mover", GUILayout.Height(30)))
            {
                ObjectMoverWindow.ShowWindow();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Object Toggler", GUILayout.Height(30)))
            {
                ObjectTogglerWindow.ShowWindow();
            }
        }
    }
}
