using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AnoGame.EditorExtensions
{
    public class ObjectPingerWindow : EditorWindow
    {
        [System.Serializable]
        private class TrackedObjectData
        {
            public string globalObjectId;
            public string cachedName;
        }

        private List<TrackedObjectData> trackedObjects = new List<TrackedObjectData>();
        private const string PREFS_KEY = "ObjectPinger_TrackedObjects";
        private Vector2 scrollPosition;
        private ReorderableList _reorderableList;

        [MenuItem("Tools/Object Pinger")]
        public static void ShowWindow()
        {
            GetWindow<ObjectPingerWindow>("Object Pinger");
        }

        private void OnEnable()
        {
            Load();
            InitializeReorderableList();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Save();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            Repaint();
        }

        private void InitializeReorderableList()
        {
            _reorderableList = new ReorderableList(trackedObjects, typeof(TrackedObjectData), true, true, true, true);

            _reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Tracked Objects");
            };

            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= trackedObjects.Count) return;

                var item = trackedObjects[index];
                rect.y += 2;
                float height = EditorGUIUtility.singleLineHeight;
                float currentX = rect.x;

                // Ping Button
                Rect pingRect = new Rect(currentX, rect.y, 50, height);
                if (GUI.Button(pingRect, "Ping", EditorStyles.miniButton))
                {
                    PingObject(item.globalObjectId);
                }
                currentX += 55;

                // Name
                Rect nameRect = new Rect(currentX, rect.y, rect.width - (currentX - rect.x), height);
                EditorGUI.LabelField(nameRect, item.cachedName);
            };

            _reorderableList.onAddCallback = (ReorderableList list) =>
            {
                AddSelected();
            };

            _reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                if (list.index >= 0 && list.index < trackedObjects.Count)
                {
                    trackedObjects.RemoveAt(list.index);
                    Save();
                }
            };

            _reorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
            {
                Save();
            };
        }

        private void OnGUI()
        {
            GUILayout.Label("Object Pinger (Drag to Reorder)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawControlPanel();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (_reorderableList != null)
            {
                _reorderableList.DoLayoutList();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawControlPanel()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected", GUILayout.Height(30)))
            {
                AddSelected();
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                trackedObjects.Clear();
                Save();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AddSelected()
        {
            bool added = false;
            foreach (var obj in Selection.objects)
            {
                if (obj == null) continue;

                GameObject go = obj as GameObject;
                if (go == null && obj is Component comp)
                {
                    go = comp.gameObject;
                }

                if (go != null)
                {
                    string gid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

                    if (trackedObjects.Exists(x => x.globalObjectId == gid)) continue;

                    trackedObjects.Add(new TrackedObjectData
                    {
                        globalObjectId = gid,
                        cachedName = go.name
                    });
                    added = true;
                }
            }
            if (added)
            {
                Save();
                Repaint();
            }
        }

        private void PingObject(string gidStr)
        {
            if (GlobalObjectId.TryParse(gidStr, out GlobalObjectId gid))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
                else
                {
                    Debug.LogWarning($"Object with ID {gidStr} not found. It might be in an unloaded scene.");
                }
            }
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(new SerializationWrapper { items = trackedObjects });
            EditorPrefs.SetString(PREFS_KEY, json);
        }

        private void Load()
        {
            if (EditorPrefs.HasKey(PREFS_KEY))
            {
                string json = EditorPrefs.GetString(PREFS_KEY);
                var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
                if (wrapper != null && wrapper.items != null)
                {
                    trackedObjects = wrapper.items;
                }
            }
            if (trackedObjects == null) trackedObjects = new List<TrackedObjectData>();
        }

        [System.Serializable]
        private class SerializationWrapper
        {
            public List<TrackedObjectData> items;
        }
    }
}
