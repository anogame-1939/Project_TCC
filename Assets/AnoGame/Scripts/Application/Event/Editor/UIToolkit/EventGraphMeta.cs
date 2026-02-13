#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnoGame.Application.Event.Editor.UIToolkit
{
    [Serializable]
    public class EventGraphMeta
    {
        public int version = 1;
        public string storyId = "Story2";
        public string eventDataFolder = "Assets/AnoGame/Data/Events/Story2";
        public List<string> targetScenes = new List<string>();
        public List<NodePositionEntry> nodePositions = new List<NodePositionEntry>();
        public List<SceneBindingEntry> sceneBindings = new List<SceneBindingEntry>();

        // --- Serialization helpers ---

        private const string META_PATH = "Assets/AnoGame/Data/Events/Story2/EventGraphMeta.json";

        public static string GetMetaPath() => META_PATH;

        public static EventGraphMeta Load()
        {
            if (!File.Exists(META_PATH))
            {
                return new EventGraphMeta();
            }

            try
            {
                string json = File.ReadAllText(META_PATH);
                var meta = JsonUtility.FromJson<EventGraphMeta>(json);
                return meta ?? new EventGraphMeta();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load EventGraphMeta: {ex.Message}");
                return new EventGraphMeta();
            }
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(META_PATH);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(META_PATH, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save EventGraphMeta: {ex.Message}");
            }
        }

        // --- Lookup helpers ---

        public Vector2? GetNodePosition(string eventId)
        {
            foreach (var entry in nodePositions)
            {
                if (entry.eventId == eventId)
                    return new Vector2(entry.x, entry.y);
            }
            return null;
        }

        public void SetNodePosition(string eventId, Vector2 pos)
        {
            for (int i = 0; i < nodePositions.Count; i++)
            {
                if (nodePositions[i].eventId == eventId)
                {
                    nodePositions[i] = new NodePositionEntry { eventId = eventId, x = pos.x, y = pos.y };
                    return;
                }
            }
            nodePositions.Add(new NodePositionEntry { eventId = eventId, x = pos.x, y = pos.y });
        }

        public SceneBindingEntry GetSceneBinding(string eventId)
        {
            foreach (var entry in sceneBindings)
            {
                if (entry.eventId == eventId)
                    return entry;
            }
            return null;
        }

        public void SetSceneBinding(string eventId, string sceneName, string containerPath, string receptorName, string triggerName)
        {
            for (int i = 0; i < sceneBindings.Count; i++)
            {
                if (sceneBindings[i].eventId == eventId)
                {
                    sceneBindings[i] = new SceneBindingEntry
                    {
                        eventId = eventId,
                        sceneName = sceneName,
                        containerPath = containerPath,
                        receptorName = receptorName,
                        triggerName = triggerName
                    };
                    return;
                }
            }
            sceneBindings.Add(new SceneBindingEntry
            {
                eventId = eventId,
                sceneName = sceneName,
                containerPath = containerPath,
                receptorName = receptorName,
                triggerName = triggerName
            });
        }
    }

    [Serializable]
    public class NodePositionEntry
    {
        public string eventId;
        public float x;
        public float y;
    }

    [Serializable]
    public class SceneBindingEntry
    {
        public string eventId;
        public string sceneName;
        public string containerPath;
        public string receptorName;
        public string triggerName;
    }
}
#endif
