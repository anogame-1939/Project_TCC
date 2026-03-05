using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AnoGame.EditorExtensions
{
    public class ObjectTogglerWindow : EditorWindow
    {
        [System.Serializable]
        private class TrackedObjectData
        {
            public string globalObjectId;
            public string cachedName;
            public bool forceActiveOnPlay;
        }

        private List<TrackedObjectData> trackedObjects = new List<TrackedObjectData>();
        private const string PREFS_KEY = "ObjectToggler_TrackedObjects";
        private Vector2 scrollPosition;
        private ReorderableList _reorderableList;

        [MenuItem("Tools/Object Toggler")]
        public static void ShowWindow()
        {
            GetWindow<ObjectTogglerWindow>("Object Toggler");
        }

        private void OnEnable()
        {
            Load();
            InitializeReorderableList();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Save();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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

                // Columns layout
                float currentX = rect.x;
                float height = EditorGUIUtility.singleLineHeight;

                // Ping Button (Width 45)
                Rect pingRect = new Rect(currentX, rect.y, 45, height);
                if (GUI.Button(pingRect, "Ping", EditorStyles.miniButton))
                {
                    PingObject(item);
                }
                currentX += 50; // 45 + 5 padding

                // Resolve Object
                GameObject obj = ResolveObject(item);
                bool objectFound = obj != null;

                if (objectFound && item.cachedName != obj.name)
                {
                    item.cachedName = obj.name;
                }

                // Active Toggle (Width 20)
                Rect activeRect = new Rect(currentX, rect.y, 20, height);
                bool currentObjActive = objectFound ? obj.activeSelf : false;

                EditorGUI.BeginDisabledGroup(!objectFound);
                bool newActive = EditorGUI.Toggle(activeRect, currentObjActive);
                if (objectFound && newActive != currentObjActive)
                {
                    Undo.RecordObject(obj, "Toggle Active");
                    obj.SetActive(newActive);
                }
                EditorGUI.EndDisabledGroup();
                currentX += 25;

                // PlayON Toggle (Width 40)
                Rect playOnRect = new Rect(currentX, rect.y, 50, height);
                // Label for checkbox? No, just checkbox. Let's add tooltip or something? 
                // Space is tight. Just the checkbox.
                // Or maybe text "Play"?

                // Let's match the header: "PlayON"
                bool newForce = EditorGUI.ToggleLeft(playOnRect, new GUIContent("Play", "Force Active On Play"), item.forceActiveOnPlay);
                if (newForce != item.forceActiveOnPlay)
                {
                    item.forceActiveOnPlay = newForce;
                    Save();
                }
                currentX += 50;

                // Name
                Rect nameRect = new Rect(currentX, rect.y, rect.width - (currentX - rect.x), height);
                string displayName = objectFound ? obj.name : $"{item.cachedName} (Missing)";
                EditorGUI.LabelField(nameRect, displayName, objectFound ? EditorStyles.label : EditorStyles.wordWrappedLabel);
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

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                foreach (var item in trackedObjects)
                {
                    if (item.forceActiveOnPlay)
                    {
                        GameObject obj = ResolveObject(item);
                        if (obj != null && !obj.activeSelf)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
            }
        }

        private GameObject ResolveObject(TrackedObjectData item)
        {
            if (GlobalObjectId.TryParse(item.globalObjectId, out GlobalObjectId gid))
            {
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
            }
            return null;
        }

        private void PingObject(TrackedObjectData item)
        {
            GameObject obj = ResolveObject(item);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            else
            {
                Debug.LogWarning($"Object '{item.cachedName}' not found or unloaded.");
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Object Toggler (Drag to Reorder)", EditorStyles.boldLabel);
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
            EditorGUILayout.BeginVertical("box");

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

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All ON", GUILayout.Height(25)))
            {
                SetAllActive(true);
            }
            if (GUILayout.Button("All OFF", GUILayout.Height(25)))
            {
                SetAllActive(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
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
                        cachedName = go.name,
                        forceActiveOnPlay = false
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

        private void SetAllActive(bool active)
        {
            foreach (var item in trackedObjects)
            {
                GameObject obj = ResolveObject(item);
                if (obj != null && obj.activeSelf != active)
                {
                    Undo.RecordObject(obj, active ? "Set Active" : "Set Inactive");
                    obj.SetActive(active);
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
