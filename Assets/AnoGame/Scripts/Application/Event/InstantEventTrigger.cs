using UnityEngine;
using VContainer;
using AnoGame.Data;
using AnoGame.Application.Attributes;
using System.Reflection;

namespace AnoGame.AnoFlow
{
    /// <summary>
    /// イベントがスタートしたら即時にクリアする
    /// (拡張: IDを文字列指定してランタイムでEventDataを生成して動作)
    /// </summary>
    [DefaultExecutionOrder(999)]
    public class InstantEventTrigger : EventTriggerBase
    {
        [Header("Override")]
        [EventSelector]
        public string targetEventId;

        [SerializeField]
        private bool _onStart = false;

        [Inject] private EventManager _eventManager;

        [Inject]
        public void Construct(
            EventManager eventManager
        )
        {
            _eventManager = eventManager;
        }

        protected override void Start()
        {
            // EventDataが未設定で、targetEventIdがある場合、ランタイム生成で補完
            // (OnValidateで設定されているケースが基本だが、ランタイム生成も予備として残す)
            if (eventData == null && !string.IsNullOrEmpty(targetEventId))
            {
                eventData = ScriptableObject.CreateInstance<EventData>();
                // private field 'eventId' にアクセスしてセット
                var field = typeof(EventData).GetField("eventId", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) field.SetValue(eventData, targetEventId);
            }

            base.Start();
            if (_onStart && eventData != null)
            {
                // イベント開始をトリガーする
                base._eventService.TriggerEventStart(eventData.EventId);
            }
        }

        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            // Debug.Log($"InstantEventTrigger-OnStartEvent:{name}", this);

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
            // Debug.Log($"InstantEventTrigger-OnCompleteEvent:{name}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(targetEventId))
            {
                // すでに設定済みでIDが一致するなら何もしない
                if (eventData != null && eventData.EventId == targetEventId) return;

                // IDからアセットを検索して割り当てる
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