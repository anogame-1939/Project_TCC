using UnityEditor;
using UnityEngine;
using System;

namespace AnoGame.AnoNarrative.Editor
{
    public class TextInputPopup : EditorWindow
    {
        private string _instruction;
        private string _inputText;
        private string _placeholderText;
        private Action<string> _onConfirm;

        public static void Show(string title, string instruction, string defaultText, string placeholder, Action<string> onConfirm)
        {
            var window = ScriptableObject.CreateInstance<TextInputPopup>();
            window.titleContent = new GUIContent(title);
            window._instruction = instruction;
            window._inputText = defaultText;
            window._placeholderText = placeholder;
            window._onConfirm = onConfirm;

            // Size
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 120);
            window.ShowUtility();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(_instruction, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);

            GUI.SetNextControlName("InputField");
            _inputText = EditorGUILayout.TextField(_inputText);
            if (string.IsNullOrEmpty(_inputText) && !string.IsNullOrEmpty(_placeholderText))
            {
                // Simple placeholder visualization if empty (optional, Unity doesnt have native placeholder for TextField easily)
                Rect r = GUILayoutUtility.GetLastRect();
                EditorGUI.LabelField(r, _placeholderText, new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray }, fontStyle = FontStyle.Italic });
            }

            // Focus on start
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            {
                GUI.FocusControl("InputField");
            }

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(70)))
            {
                Close();
            }
            if (GUILayout.Button("OK", GUILayout.Width(70)))
            {
                Confirm();
            }
            GUILayout.EndHorizontal();

            // Handle Enter key
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                Confirm();
                Event.current.Use();
            }
            // Handle Esc key
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }

        private void Confirm()
        {
            string result = _inputText;
            if (string.IsNullOrEmpty(result)) result = _placeholderText;

            _onConfirm?.Invoke(result);
            Close();
        }
    }
}
