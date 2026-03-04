#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnoGame.AnoFlow.Editor
{
    [Serializable]
    public class EventGraphMeta
    {
        private const string META_FILENAME = "EventGraphMeta.json";

        public int version = 1;
        public string storyId = "";
        public string eventDataFolder = "";
        public List<string> targetScenes = new List<string>();
        public List<NodePositionEntry> nodePositions = new List<NodePositionEntry>();
        public List<SceneBindingEntry> sceneBindings = new List<SceneBindingEntry>();

        // --- Serialization helpers ---

        /// <summary>
        /// 指定フォルダのメタファイルパスを返す
        /// </summary>
        public static string GetMetaPath(string folderPath)
        {
            return $"{folderPath}/{META_FILENAME}";
        }

        /// <summary>
        /// 指定フォルダからメタデータを読み込む
        /// </summary>
        public static EventGraphMeta Load(string folderPath)
        {
            string metaPath = GetMetaPath(folderPath);
            if (!File.Exists(metaPath))
            {
                var newMeta = new EventGraphMeta();
                newMeta.eventDataFolder = folderPath;
                newMeta.storyId = Path.GetFileName(folderPath);
                return newMeta;
            }

            try
            {
                string json = File.ReadAllText(metaPath);
                var meta = JsonUtility.FromJson<EventGraphMeta>(json);
                if (meta == null)
                {
                    meta = new EventGraphMeta();
                    meta.eventDataFolder = folderPath;
                    meta.storyId = Path.GetFileName(folderPath);
                }
                return meta;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load EventGraphMeta: {ex.Message}");
                var fallback = new EventGraphMeta();
                fallback.eventDataFolder = folderPath;
                fallback.storyId = Path.GetFileName(folderPath);
                return fallback;
            }
        }

        /// <summary>
        /// 指定フォルダにメタデータを保存する
        /// </summary>
        public void Save(string folderPath)
        {
            try
            {
                string metaPath = GetMetaPath(folderPath);
                string dir = Path.GetDirectoryName(metaPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(metaPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save EventGraphMeta: {ex.Message}");
            }
        }

        // --- Lookup helpers ---

        public Vector2? GetNodePosition(string guid)
        {
            foreach (var entry in nodePositions)
            {
                if (entry.guid == guid)
                    return new Vector2(entry.x, entry.y);
            }
            return null;
        }

        public void SetNodePosition(string guid, string eventId, Vector2 pos)
        {
            for (int i = 0; i < nodePositions.Count; i++)
            {
                if (nodePositions[i].guid == guid)
                {
                    nodePositions[i] = new NodePositionEntry { guid = guid, eventId = eventId, x = pos.x, y = pos.y };
                    return;
                }
            }
            nodePositions.Add(new NodePositionEntry { guid = guid, eventId = eventId, x = pos.x, y = pos.y });
        }

        /// <summary>
        /// eventId で座標を検索（旧形式JSON互換のフォールバック用）
        /// </summary>
        public Vector2? GetNodePositionByEventId(string eventId)
        {
            foreach (var entry in nodePositions)
            {
                if (entry.eventId == eventId && string.IsNullOrEmpty(entry.guid))
                    return new Vector2(entry.x, entry.y);
            }
            return null;
        }

        public void RemoveNodePosition(string guid)
        {
            nodePositions.RemoveAll(e => e.guid == guid);
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
        public string guid;
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
