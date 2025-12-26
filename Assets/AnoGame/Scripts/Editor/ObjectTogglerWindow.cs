using System.Collections.Generic;
using UnityEditor;
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

        [MenuItem("Tools/Object Toggler")]
        public static void ShowWindow()
        {
            GetWindow<ObjectTogglerWindow>("Object Toggler");
        }

        private void OnEnable()
        {
            Load();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Save();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                foreach (var item in trackedObjects)
                {
                    if (item.forceActiveOnPlay)
                    {
                        if (GlobalObjectId.TryParse(item.globalObjectId, out GlobalObjectId gid))
                        {
                            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
                            if (obj != null && !obj.activeSelf)
                            {
                                obj.SetActive(true);
                            }
                        }
                    }
                }

                // If we modified something, we might want to ensure it's saved/handled before play starts?
                // Actually, simple SetActive is enough. 
                // However, since we are exiting edit mode, these changes might persist or be reset depending on exactly when they happen vs scene save.
                // Usually changing before play starts (ExitingEditMode) means it enters play mode with that state.
                // If the user didn't save the scene, it might prompt? or just apply. 
            }
        }

        private void OnGUI()
        {
            DrawControlPanel();
            EditorGUILayout.Space();
            DrawObjectList();
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

        private void DrawObjectList()
        {
            EditorGUILayout.LabelField("Tracked Objects", EditorStyles.boldLabel);

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(45)); // Ping
            EditorGUILayout.LabelField("Active", GUILayout.Width(45));
            EditorGUILayout.LabelField("PlayON", GUILayout.Width(50));
            EditorGUILayout.LabelField("Name", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("", GUILayout.Width(25)); // X
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < trackedObjects.Count; i++)
            {
                var item = trackedObjects[i];
                DrawObjectRow(item, i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawObjectRow(TrackedObjectData item, int index)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Resolve Object
            GameObject obj = null;
            if (GlobalObjectId.TryParse(item.globalObjectId, out GlobalObjectId gid))
            {
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
            }

            bool objectFound = obj != null;

            // Ping Button
            if (GUILayout.Button("Ping", GUILayout.Width(45)))
            {
                if (objectFound)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
                else
                {
                    Debug.LogWarning($"Object '{item.cachedName}' not found or unloaded.");
                }
            }

            // Toggle Active
            bool isActive = false;

            if (objectFound)
            {
                isActive = obj.activeSelf;
            }

            EditorGUI.BeginDisabledGroup(!objectFound);

            // Active Toggle
            bool newActive = EditorGUILayout.Toggle(isActive, GUILayout.Width(45));
            if (objectFound && newActive != isActive)
            {
                Undo.RecordObject(obj, "Toggle Active");
                obj.SetActive(newActive);
            }

            // Force Active On Play Toggle
            bool newForceActive = EditorGUILayout.Toggle(item.forceActiveOnPlay, GUILayout.Width(50));
            if (newForceActive != item.forceActiveOnPlay)
            {
                item.forceActiveOnPlay = newForceActive;
                Save(); // Save preference change immediately
            }

            EditorGUI.EndDisabledGroup();

            // Object Name Label
            string displayName = objectFound ? obj.name : $"{item.cachedName} (Missing)";
            if (objectFound)
            {
                // Update cached name if changed
                if (item.cachedName != obj.name)
                {
                    item.cachedName = obj.name;
                    // We don't save immediately for name changes to avoid perf hit, 
                    // but it will save on close/add/remove.
                }
            }

            EditorGUILayout.LabelField(displayName, GUILayout.ExpandWidth(true));

            // Remove Button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                trackedObjects.RemoveAt(index);
                Save();
                GUIUtility.ExitGUI(); // Stop drawing this frame since list changed
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddSelected()
        {
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
                }
            }
            Save();
        }

        private void SetAllActive(bool active)
        {
            foreach (var item in trackedObjects)
            {
                if (GlobalObjectId.TryParse(item.globalObjectId, out GlobalObjectId gid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
                    if (obj != null && obj.activeSelf != active)
                    {
                        Undo.RecordObject(obj, active ? "Set Active" : "Set Inactive");
                        obj.SetActive(active);
                    }
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
        }

        [System.Serializable]
        private class SerializationWrapper
        {
            public List<TrackedObjectData> items;
        }
    }
}
