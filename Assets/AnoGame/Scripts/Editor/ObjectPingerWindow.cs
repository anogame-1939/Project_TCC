using System.Collections.Generic;
using UnityEditor;
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

        [MenuItem("Tools/Object Pinger")]
        public static void ShowWindow()
        {
            GetWindow<ObjectPingerWindow>("Object Pinger");
        }

        private void OnEnable()
        {
            Load();
        }

        private void OnDisable()
        {
            Save();
        }

        private void OnGUI()
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tracked Objects", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < trackedObjects.Count; i++)
            {
                var item = trackedObjects[i];
                EditorGUILayout.BeginHorizontal("box");

                // Ping Button
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    PingObject(item.globalObjectId);
                }

                // Object Name Label
                EditorGUILayout.LabelField(item.cachedName, GUILayout.ExpandWidth(true));

                // Remove Button
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    trackedObjects.RemoveAt(i);
                    Save();
                    i--; 
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void AddSelected()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj == null) continue;

                // Only track GameObjects or Components (convert to GameObject)
                GameObject go = obj as GameObject;
                if (go == null && obj is Component comp)
                {
                    go = comp.gameObject;
                }

                if (go != null)
                {
                    string gid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
                    
                    // Avoid duplicates
                    if (trackedObjects.Exists(x => x.globalObjectId == gid)) continue;

                    trackedObjects.Add(new TrackedObjectData
                    {
                        globalObjectId = gid,
                        cachedName = go.name
                    });
                }
            }
            Save();
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
        }

        [System.Serializable]
        private class SerializationWrapper
        {
            public List<TrackedObjectData> items;
        }
    }
}
