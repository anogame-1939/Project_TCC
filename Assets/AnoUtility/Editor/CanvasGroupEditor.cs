using UnityEngine;
using UnityEditor;

namespace AnoGame.Utility.Editor
{
    /// <summary>
    /// CanvasGroup のカスタムインスペクター。
    /// トグルボタンで alpha / interactable / blocksRaycasts を一括切替できる。
    /// </summary>
    [CustomEditor(typeof(CanvasGroup))]
    public class CanvasGroupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var canvasGroup = (CanvasGroup)target;

            // ── Toggle Button ──
            bool isActive = canvasGroup.alpha > 0.5f;
            string label = isActive ? "▼ Active" : "▲ Inactive";

            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = isActive
                ? new Color(0.3f, 0.7f, 0.3f)
                : new Color(0.5f, 0.5f, 0.5f);

            if (GUILayout.Button(label, GUILayout.Height(24)))
            {
                Undo.RecordObject(canvasGroup, "Toggle CanvasGroup");
                bool newState = !isActive;
                canvasGroup.alpha = newState ? 1f : 0f;
                canvasGroup.interactable = newState;
                canvasGroup.blocksRaycasts = newState;
                EditorUtility.SetDirty(canvasGroup);
            }

            GUI.backgroundColor = bgColor;

            EditorGUILayout.Space(4);

            // ── Default Inspector ──
            DrawDefaultInspector();
        }
    }
}
