using UnityEngine;
using AnoGame.Domain.Event.Types;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
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

        /// <summary>
        /// このイベントが実行可能かどうかを判定する。
        /// 条件判定を EventData に一元化し、Receptor/Trigger から呼び出す。
        /// </summary>
        public bool CanExecute(IEventService eventService, IInventoryService inventoryService)
        {
            // 1. OneTime かつクリア済みなら実行不可
            if (isOneTime && eventService.IsEventCleared(eventId))
                return false;

            // 2. 必要アイテムの所持チェック
            if (requiredItemIds != null)
            {
                foreach (var item in requiredItemIds)
                {
                    if (!string.IsNullOrEmpty(item.itemId) && !inventoryService.HasItem(item.itemId))
                        return false;
                }
            }

            // 3. 必要イベントのクリアチェック
            if (requiredEventIds != null)
            {
                foreach (var evt in requiredEventIds)
                {
                    if (!string.IsNullOrEmpty(evt.eventId) && !eventService.IsEventCleared(evt.eventId))
                        return false;
                }
            }

            // 4. タグ条件チェック（ネガティブタグ "!" prefix 対応）
            if (conditionTags != null)
            {
                foreach (var tag in conditionTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;

                    if (tag.StartsWith("!"))
                    {
                        // ネガティブ: タグが付いていなければOK
                        if (eventService.HasTag(tag.Substring(1)))
                            return false;
                    }
                    else
                    {
                        // ポジティブ: タグが付いていればOK
                        if (!eventService.HasTag(tag))
                            return false;
                    }
                }
            }

            return true;
        }

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