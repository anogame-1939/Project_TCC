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
        [SerializeField] private string category;
        [SerializeField] private string description;
        [SerializeField] private bool isOneTime; // 一回限りのイベントかどうか

        public string EventId => eventId;
        public string EventName => eventName;
        public string Category => category;
        public string Description => description;
        public bool IsOneTime => isOneTime;

        [Header("Conditions")]
        [SerializeField] private List<ItemCondition> requiredItemIds = new List<ItemCondition>();
        [SerializeField] private List<EventCondition> requiredEventIds = new List<EventCondition>();

        [Header("Results")]
        [SerializeField] private List<string> results = new List<string>();

        public List<string> RequiredItemIds => System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(requiredItemIds, x => x.itemId));
        public List<string> RequiredEventIds => System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(requiredEventIds, x => x.eventId));
        public List<string> Results => results != null ? new List<string>(results) : new List<string>();

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

    [System.Serializable]
    public class ItemCondition
    {
        [ItemSelector] public string itemId;
    }

    [System.Serializable]
    public class EventCondition
    {
        [EventSelector] public string eventId;
    }
}