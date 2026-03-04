#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AnoGame.Application.Event;
using System.Collections.Generic;

namespace AnoGame.AnoFlow.Editor
{
    /// <summary>
    /// Utility to find and ping scene GameObjects linked to an EventData by eventId.
    /// Uses AnoEventRoot component references for reliable scene binding.
    /// </summary>
    public static class EventSceneBinder
    {
        /// <summary>
        /// Find the container GameObject for the given eventId in loaded scenes.
        /// Uses AnoEventRoot.EventData reference for GUID-based lookup.
        /// </summary>
        public static GameObject FindContainer(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;

            var roots = Object.FindObjectsByType<AnoEventRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var root in roots)
            {
                if (root.EventData != null && root.EventData.EventId == eventId)
                {
                    return root.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Find ALL container GameObjects for the given eventId in loaded scenes.
        /// Used for duplicate detection.
        /// </summary>
        public static List<GameObject> FindAllContainers(string eventId)
        {
            var result = new List<GameObject>();
            if (string.IsNullOrEmpty(eventId)) return result;

            var roots = Object.FindObjectsByType<AnoEventRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var root in roots)
            {
                if (root.EventData != null && root.EventData.EventId == eventId)
                {
                    result.Add(root.gameObject);
                }
            }
            return result;
        }

        /// <summary>
        /// Find the Receptor child under the event container.
        /// Uses AnoEventRoot.Receptor reference.
        /// </summary>
        public static GameObject FindReceptor(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;

            var roots = Object.FindObjectsByType<AnoEventRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var root in roots)
            {
                if (root.EventData != null && root.EventData.EventId == eventId)
                {
                    return root.Receptor;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the Trigger child under the event container.
        /// Uses AnoEventRoot.Trigger reference.
        /// </summary>
        public static GameObject FindTrigger(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;

            var roots = Object.FindObjectsByType<AnoEventRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var root in roots)
            {
                if (root.EventData != null && root.EventData.EventId == eventId)
                {
                    return root.Trigger;
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
