#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace AnoGame.AnoFlow.Editor
{
    /// <summary>
    /// Utility to find and ping scene GameObjects linked to an EventData by eventId.
    /// Uses hybrid approach: name-convention search + meta file fallback.
    /// </summary>
    public static class EventSceneBinder
    {
        private static readonly string[] ROOT_NAMES = { "GeneratedEvents", "GeneratedEventsV2" };

        /// <summary>
        /// Find the container GameObject for the given eventId in loaded scenes.
        /// </summary>
        public static GameObject FindContainer(string eventId)
        {
            foreach (var rootName in ROOT_NAMES)
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;

                foreach (Transform child in root.transform)
                {
                    if (child.name.StartsWith(eventId))
                    {
                        return child.gameObject;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find the Receptor child under the event container.
        /// </summary>
        public static GameObject FindReceptor(string eventId)
        {
            var container = FindContainer(eventId);
            if (container == null) return null;

            foreach (Transform child in container.transform)
            {
                if (child.name.Contains("Receptor"))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the Trigger child under the event container.
        /// </summary>
        public static GameObject FindTrigger(string eventId)
        {
            var container = FindContainer(eventId);
            if (container == null) return null;

            foreach (Transform child in container.transform)
            {
                if (child.name.Contains("Trigger"))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Ping the container in the Hierarchy.
        /// </summary>
        public static void PingContainer(string eventId)
        {
            var go = FindContainer(eventId);
            if (go != null)
            {
                EditorGUIUtility.PingObject(go);
            }
            else
            {
                Debug.LogWarning($"EventSceneBinder: Container not found for {eventId} in loaded scenes.");
            }
        }

        /// <summary>
        /// Select and focus on the container in the Hierarchy.
        /// </summary>
        public static void SelectContainer(string eventId)
        {
            var go = FindContainer(eventId);
            if (go != null)
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);

                // シーンビューのカメラを選択オブジェクトにフレーミング
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
            else
            {
                Debug.LogWarning($"EventSceneBinder: Container not found for {eventId} in loaded scenes.");
            }
        }

        /// <summary>
        /// Ping the Receptor in the Hierarchy.
        /// </summary>
        public static void PingReceptor(string eventId)
        {
            var go = FindReceptor(eventId);
            if (go != null)
            {
                EditorGUIUtility.PingObject(go);
            }
            else
            {
                Debug.LogWarning($"EventSceneBinder: Receptor not found for {eventId}.");
            }
        }

        /// <summary>
        /// Ping the Trigger in the Hierarchy.
        /// </summary>
        public static void PingTrigger(string eventId)
        {
            var go = FindTrigger(eventId);
            if (go != null)
            {
                EditorGUIUtility.PingObject(go);
            }
            else
            {
                Debug.LogWarning($"EventSceneBinder: Trigger not found for {eventId}.");
            }
        }
    }
}
#endif
