using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace AnoGame.AnoDialogue.Editor
{
    public class SectionCreatePopup : EditorWindow
    {
        private MasterDialogueData _data;
        private int _episodeID;
        private int _chapterID;
        private int _sectionID;
        private string _sectionName;
        private Action<int, int, int, string> _onConfirm;

        public static void Show(string title, MasterDialogueData data, int defaultEp, int defaultCh, int defaultSec, string defaultName, Action<int, int, int, string> onConfirm)
        {
            var window = ScriptableObject.CreateInstance<SectionCreatePopup>();
            window.titleContent = new GUIContent(title);
            window._data = data;

            // Set defaults
            window._episodeID = defaultEp;
            window._chapterID = defaultCh;
            window._sectionID = defaultSec;
            window._sectionName = defaultName;

            window._onConfirm = onConfirm;

            // Size
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 200);
            window.ShowUtility();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Section", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _episodeID = EditorGUILayout.IntField("Episode ID (Ep)", _episodeID);
            _chapterID = EditorGUILayout.IntField("Chapter ID (Ch)", _chapterID);
            _sectionID = EditorGUILayout.IntField("Section ID (Sec)", _sectionID);

            // Helpful note if -1
            if (_episodeID == 0 || _chapterID == 0 || _sectionID == 0)
            {
                EditorGUILayout.HelpBox("0 will be treated as Default.", MessageType.Info);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Section Name");
            GUI.SetNextControlName("SectionNameField");
            _sectionName = EditorGUILayout.TextField(_sectionName);

            // Validation Logic (Visual Only)
            if (_data != null)
            {
                // Check if this combination already exists
                bool exists = _data.Conversations.Any(u =>
                    u.EpisodeID == _episodeID &&
                    u.ChapterID == _chapterID &&
                    u.SectionID == _sectionID);

                if (exists)
                {
                    EditorGUILayout.HelpBox($"A Section with ID {_sectionID} already exists in Ep {_episodeID}, Ch {_chapterID}. It will be incremented automatically if you proceed.", MessageType.Warning);
                }
            }

            // Focus on name if IDs are pre-filled? Or usually user might want to tab through.
            // Let's focus Name if it's empty, otherwise first int field? 
            // Default focus on SectionName might be best if we auto-filled IDs.
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            {
                GUI.FocusControl("SectionNameField");
            }

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                Close();
            }
            if (GUILayout.Button("Create", GUILayout.Width(80)))
            {
                Confirm();
            }
            GUILayout.EndHorizontal();

            // Handle Key Events
            if (Event.current.isKey)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    Confirm();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
            }
        }

        private void Confirm()
        {
            // Empty Int Check logic?
            // "入力がない値は-1とし" -> The fields are IntFields, so they will be 0 if cleared usually, or previous value.
            // If the user wants -1, they type -1. 
            // Limitation of EditorGUILayout.IntField: it doesn't support "Empty". 
            // But user said "input empty is -1". 
            // In Unity IntField, you can't really have "Empty". 
            // So we'll assume the default passed in is -1. If they see 0, they might change it.
            // We'll stick to what the field says. 

            // Check conflicts
            int finalSecID = _sectionID;
            if (_data != null)
            {
                // "既存のIDと被ってたらインクリメント" (If overlaps, increment)
                while (_data.Conversations.Any(u => u.EpisodeID == _episodeID && u.ChapterID == _chapterID && u.SectionID == finalSecID))
                {
                    finalSecID++;
                }
            }

            // Name fallback
            string finalName = _sectionName;
            if (string.IsNullOrEmpty(finalName))
            {
                // Assign Default? 
                finalName = $"NewSection_{finalSecID}";
            }

            _onConfirm?.Invoke(_episodeID, _chapterID, finalSecID, finalName);
            Close();
        }
    }
}
