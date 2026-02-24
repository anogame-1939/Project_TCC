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
        [SerializeField] private bool isOneTime;

        [HideInInspector]
        [SerializeField] private bool _isDeleted;
        public bool IsDeleted { get => _isDeleted; set => _isDeleted = value; }

        public string EventId => eventId;
        public string EventName => eventName;
        public string Category => category;
        public string Description => description;
        public bool IsOneTime => isOneTime;

        [Header("Conditions (Legacy: Direct Event/Item References)")]
        [SerializeField] private List<ItemCondition> requiredItemIds = new List<ItemCondition>();
        [SerializeField] private List<EventCondition> requiredEventIds = new List<EventCondition>();

        [Header("Tags (Narrative-based Conditions)")]
        [Tooltip("このイベントの前提条件となるタグ（例: Has_Magnet, Knowledge_LockerCode）")]
        [SerializeField] private List<string> conditionTags = new List<string>();

        [Tooltip("このイベント完了で獲得するタグ（例: State_Door1_Open, Has_ControlKey）")]
        [SerializeField] private List<string> resultTags = new List<string>();

        public List<string> RequiredItemIds => System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(requiredItemIds, x => x.itemId));
        public List<string> RequiredEventIds => System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(requiredEventIds, x => x.eventId));
        public List<string> ConditionTags => conditionTags != null ? new List<string>(conditionTags) : new List<string>();
        public List<string> ResultTags => resultTags != null ? new List<string>(resultTags) : new List<string>();

        public bool HasConditions => (requiredItemIds != null && requiredItemIds.Count > 0)
            || (requiredEventIds != null && requiredEventIds.Count > 0)
            || (conditionTags != null && conditionTags.Count > 0);

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