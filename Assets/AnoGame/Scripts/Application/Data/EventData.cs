using UnityEngine;
using AnoGame.Domain.Event.Types;
using System.Collections.Generic;
using AnoGame.Application.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Data
{
    [CreateAssetMenu(fileName = "EventData", menuName = "Game/EventData")]
    public class EventData : ScriptableObject, IEventSettings
    {
        [SerializeField] private string eventId;
        [SerializeField] private string eventName;
        [SerializeField] private string description;
        [SerializeField] private bool isOneTime; // 一回限りのイベントかどうか

        public string EventId => eventId;
        public string EventName => eventName;
        public string Description => description;
        public bool IsOneTime => isOneTime;

        [Header("Conditions")]
        [SerializeField, ItemSelector] private List<string> requiredItemIds = new List<string>();
        [SerializeField, EventSelector] private List<string> requiredEventIds = new List<string>();

        public List<string> RequiredItemIds => requiredItemIds;
        public List<string> RequiredEventIds => requiredEventIds;

        public bool HasConditions => (requiredItemIds != null && requiredItemIds.Count > 0) || (requiredEventIds != null && requiredEventIds.Count > 0);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(eventId))
            {
                eventId = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}