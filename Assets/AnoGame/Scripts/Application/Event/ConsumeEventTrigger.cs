using UnityEngine;
using VContainer;
using AnoGame.Data;
using AnoGame.Application.Attributes;
using AnoGame.Domain.Event.Conditions;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Event.Services;
using System.Collections.Generic;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// ItemReceptor専用のイベントトリガー。
    /// InstantEventTriggerと異なり、OnConditionChanged による自動発火を行わない。
    /// イベントは ItemReceptor.TryStart() → TriggerEventStart() 経由でのみ開始される。
    /// 条件オブジェクトは CheckConditions() での判定にのみ使用する。
    /// </summary>
    [DefaultExecutionOrder(999)]
    public class ConsumeEventTrigger : EventTriggerBase
    {
        [Header("Override")]
        [EventSelector]
        public string targetEventId;

        [Inject] private EventManager _eventManager;

        [Inject]
        public void Construct(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        protected override void Start()
        {
            // EventDataが未設定で、targetEventIdがある場合、ランタイム生成で補完
            if (eventData == null && !string.IsNullOrEmpty(targetEventId))
            {
                eventData = ScriptableObject.CreateInstance<EventData>();
                var field = typeof(EventData).GetField("eventId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(eventData, targetEventId);
            }

            base.Start();
        }

        /// <summary>
        /// EventTriggerBase の InitializeConditions をオーバーライド。
        /// 条件オブジェクトは生成するが、OnConditionChanged への購読は行わない。
        /// これにより、条件変化時の自動発火を抑止する。
        /// </summary>
        protected override void InitializeConditions()
        {
            // EventData 定義の条件を登録（購読なし）
            if (eventData != null)
            {
                if (eventData.RequiredItemIds != null)
                {
                    foreach (var itemId in eventData.RequiredItemIds)
                    {
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            var condition = new KeyItemCondition(_inventoryService, itemId);
                            AddConditionWithoutSubscription(condition);
                        }
                    }
                }

                if (eventData.RequiredEventIds != null)
                {
                    foreach (var evtId in eventData.RequiredEventIds)
                    {
                        if (!string.IsNullOrEmpty(evtId))
                        {
                            var condition = new EventClearedCondition(_eventService, evtId);
                            AddConditionWithoutSubscription(condition);
                        }
                    }
                }

                // ConditionTags
                if (eventData.ConditionTags != null)
                {
                    foreach (var tag in eventData.ConditionTags)
                    {
                        if (!string.IsNullOrEmpty(tag))
                        {
                            var condition = new TagCondition(_eventService, tag);
                            AddConditionWithoutSubscription(condition);
                        }
                    }
                }
            }

            Debug.Log($"[ConsumeEventTrigger] PrepareEvent:{name} (no auto-fire subscriptions)");
        }

        protected override void OnStartEvent()
        {
            base.OnStartEvent();

            if (CheckConditions())
            {
                if (eventData == null) return;

                // スタートと同時にイベントをクリアする
                _eventService.TriggerEventComplete(eventData.EventId);
                _eventManager.AddClearedEvent(eventData.EventId);

                // ResultTags の永続化
                if (eventData.ResultTags != null && eventData.ResultTags.Count > 0)
                {
                    _eventManager.AddTags(eventData.ResultTags);
                }
            }
        }

        public override void OnFinishEvent()
        {
            base.OnFinishEvent();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(targetEventId))
            {
                if (eventData != null && eventData.EventId == targetEventId) return;

                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EventData");
                foreach (var guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    EventData asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EventData>(path);
                    if (asset != null && asset.EventId == targetEventId)
                    {
                        eventData = asset;
                        UnityEditor.EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }
        }
#endif
    }
}
