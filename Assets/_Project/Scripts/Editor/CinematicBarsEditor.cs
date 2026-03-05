using UnityEngine;
using UnityEditor;
using AnoGame.Apllication.Direction;

namespace AnoGame.Editor
{
    /// <summary>
    /// CinematicBars のカスタムインスペクター。
    /// トグルボタンでレターボックスの表示/非表示を即時切替できる。
    /// </summary>
    [CustomEditor(typeof(CinematicBars))]
    public class CinematicBarsEditor : UnityEditor.Editor
    {
        SerializedProperty _topBar;
        SerializedProperty _bottomBar;
        SerializedProperty _barHeight;

        void OnEnable()
        {
            _topBar = serializedObject.FindProperty("topBar");
            _bottomBar = serializedObject.FindProperty("bottomBar");
            _barHeight = serializedObject.FindProperty("barHeight");
        }

        public override void OnInspectorGUI()
        {
            var bars = (CinematicBars)target;

            // ── 現在の状態を判定 ──
            bool isShowing = IsShowing();

            // ── Toggle Button ──
            string label = isShowing ? "▼ Visible" : "▲ Hidden";

            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = isShowing
                ? new Color(0.3f, 0.7f, 0.3f)
                : new Color(0.5f, 0.5f, 0.5f);

            if (GUILayout.Button(label, GUILayout.Height(24)))
            {
                ToggleBars(!isShowing);
            }

            GUI.backgroundColor = bgColor;

            EditorGUILayout.Space(4);

            // ── Default Inspector ──
            DrawDefaultInspector();
        }

        bool IsShowing()
        {
            var topBar = _topBar.objectReferenceValue as RectTransform;
            var bottomBar = _bottomBar.objectReferenceValue as RectTransform;

            if (topBar != null && topBar.sizeDelta.y > 0.1f) return true;
            if (bottomBar != null && bottomBar.sizeDelta.y > 0.1f) return true;
            return false;
        }

        void ToggleBars(bool show)
        {
            var topBar = _topBar.objectReferenceValue as RectTransform;
            var bottomBar = _bottomBar.objectReferenceValue as RectTransform;
            float targetHeight = show ? _barHeight.floatValue : 0f;

            if (topBar != null)
            {
                Undo.RecordObject(topBar, "Toggle CinematicBars");
                var s = topBar.sizeDelta;
                s.y = targetHeight;
                topBar.sizeDelta = s;
                EditorUtility.SetDirty(topBar);
            }

            if (bottomBar != null)
            {
                Undo.RecordObject(bottomBar, "Toggle CinematicBars");
                var s = bottomBar.sizeDelta;
                s.y = targetHeight;
                bottomBar.sizeDelta = s;
                EditorUtility.SetDirty(bottomBar);
            }

            // シーンビューを即時更新
            SceneView.RepaintAll();
        }
    }
}
