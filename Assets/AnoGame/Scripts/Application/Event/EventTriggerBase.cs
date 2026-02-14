using UnityEngine;
using UnityEngine.Events;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Event.Types;
using AnoGame.Data;
using AnoGame.Domain.Event.Conditions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AnoGame.Application.Event
{
    public abstract class EventTriggerBase : MonoBehaviour
    {
        [SerializeField] protected EventData eventData;
        public EventData EventData => eventData;
        private IEventSettings EventSettings => eventData;
        [SerializeField] protected UnityEvent onPrepareEvent;
        [SerializeField] protected UnityEvent onEventStart;
        // クリアしてすぐのイベント
        [SerializeField] protected UnityEvent onEventFinish;
        // クリア後のイベント
        [SerializeField] protected UnityEvent onEventDone;
        [SerializeField] protected UnityEvent onEventFailed;
        [Header("Legacy")]
        [SerializeField] private List<EventData> supersededByEvents;
        [SerializeField] protected EventConditionComponent[] conditionComponents;

        [Header("Conditions")]
        [SerializeField] protected List<ItemData> requiredItems = new List<ItemData>();
        [SerializeField] protected List<EventData> requiredEvents = new List<EventData>();

        private List<IEventCondition> _conditions = new List<IEventCondition>();

        // NOTE:重複登録防止。本来ならライフサイクルで制御したいところ
        private bool _registeredEvent = false;

        [Inject] protected IEventService _eventService;

        [Inject] protected IInventoryService _inventoryService;

        [Inject]
        public virtual void Construct(IEventService eventService, IInventoryService inventoryService)
        {
            _eventService = eventService;
            _inventoryService = inventoryService;
            _eventService.LoadedClearEvent += InitializeEvents;
        }

        protected virtual void Start()
        {
            InitializeConditions();
            InitializeEvents();
        }

        [SerializeField] protected bool checkConditionsOnDone = false;



        protected virtual void InitializeEvents()
        {
            // クリア済みのイベント事項
            if (_eventService.IsEventCleared(eventData.EventId))
            {
                // すでに上位のイベントが完了していたらDoneイベントを実行しない
                if (supersededByEvents != null && supersededByEvents.Any(e => _eventService.IsEventCleared(e.EventId)))
                {
                    return;
                }

                if (checkConditionsOnDone)
                {
                    // 条件が初期化されていない場合は初期化
                    if (_conditions.Count == 0)
                    {
                        InitializeConditions();
                    }

                    if (!CheckConditions())
                    {
                        return;
                    }
                }

                OnDoneEvent();
            }
            // 未クリアならイベント登録
            else
            {
                if (!_registeredEvent)
                {
                    _eventService.RegisterStartEventHandler(eventData.EventId, OnStartEvent);
                    _eventService.RegisterCompleteEventHandler(eventData.EventId, OnFinishEvent);
                    _eventService.RegisterFailedEventHandler(eventData.EventId, OnFailedEvent);
                    _registeredEvent = true;
                }
            }
        }


        protected virtual void InitializeConditions()
        {
            // 新しいIDベースの条件を追加
            if (requiredItems != null)
            {
                foreach (var item in requiredItems)
                {
                    if (item != null)
                    {
                        var condition = new KeyItemCondition(_inventoryService, item.ItemId);
                        _conditions.Add(condition);
                        if (condition is IObservableCondition observableCondition)
                        {
                            observableCondition.OnConditionChanged += StartEvent;
                        }
                    }
                }
            }

            // EventData defined conditions
            if (eventData != null)
            {
                if (eventData.RequiredItemIds != null)
                {
                    foreach (var itemId in eventData.RequiredItemIds)
                    {
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            var condition = new KeyItemCondition(_inventoryService, itemId);
                            _conditions.Add(condition);
                            if (condition is IObservableCondition observableCondition)
                            {
                                observableCondition.OnConditionChanged += StartEvent;
                            }
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
                            _conditions.Add(condition);
                            if (condition is IObservableCondition observableCondition)
                            {
                                observableCondition.OnConditionChanged += StartEvent;
                            }
                        }
                    }
                }
            }

            if (requiredEvents != null)
            {
                foreach (var evt in requiredEvents)
                {
                    if (evt != null)
                    {
                        var condition = new EventClearedCondition(_eventService, evt.EventId);
                        _conditions.Add(condition);
                        if (condition is IObservableCondition observableCondition)
                        {
                            observableCondition.OnConditionChanged += StartEvent;
                        }
                    }
                }
            }

            // ConditionTagsベースの条件を追加
            if (eventData != null && eventData.ConditionTags != null)
            {
                foreach (var tag in eventData.ConditionTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        var condition = new TagCondition(_eventService, tag);
                        _conditions.Add(condition);
                    }
                }
            }

            if (conditionComponents == null) return;

            foreach (var component in conditionComponents)
            {
                if (component != null)
                {
                    var condition = component.CreateCondition();
                    _conditions.Add(condition);

                    // コンディションの状態変化を監視
                    if (condition is IObservableCondition observableCondition)
                    {
                        observableCondition.OnConditionChanged += StartEvent;
                    }
                }
            }

            Debug.Log("PrepareEvent:" + name);

            // 初期化のタイミングでチェック
            PrepareEvent();

            // 条件がそろっていればStartEventを実行
            // if (CheckConditions())
            {
                // StartEvent();
            }
        }

        /// <summary>
        /// NOTE:シーンロード時に必ず実行される
        /// </summary>
        private void PrepareEvent()
        {
            onPrepareEvent?.Invoke();
        }

        protected bool CheckConditions()
        {
            Debug.Log($"CheckConditions:{name} ConditionsCount:{_conditions.Count}");
            if (_conditions.Count == 0)
                return true;

            Debug.Log($"AllConditionsSatisfied:{_conditions.All(condition => condition.IsSatisfied())}");

            return _conditions.All(condition => condition.IsSatisfied());
        }

        protected virtual void OnDestroy()
        {
            foreach (var condition in _conditions)
            {
                if (condition is IObservableCondition observableCondition)
                {
                    observableCondition.OnConditionChanged -= StartEvent;
                }

                if (condition is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        public void StartEvent()
        {
            OnStartEvent();
        }

        protected virtual void OnStartEvent()
        {
            Debug.Log($"OnStartEvent:{name}");
            if (!CheckConditions())
                return;

            onEventStart?.Invoke();

            // ResultTags を付与
            if (eventData != null && eventData.ResultTags != null && eventData.ResultTags.Count > 0)
            {
                _eventService.AddTags(eventData.ResultTags);
            }
        }

        public virtual void OnFinishEvent()
        {
            Debug.Log("OnCompleteEvent");
            // if (_eventProgressService.GetEventState(eventData.EventId) != EventState.InProgress)
            // return;

            onEventFinish?.Invoke();

            if (!eventData.IsOneTime)
            {
                // _eventProgressService.ResetEvent(eventData.EventId);
            }
            // OnDoneEvent();
        }

        public virtual void OnDoneEvent()
        {
            // Debug.Log("OnDoneEvent:" + name);
            try
            {
                onEventDone?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"OnDoneEventでエラー。急場で握りつぶし。:{gameObject.name}...{e.Message}");


            }

            if (!eventData.IsOneTime)
            {
                // _eventProgressService.ResetEvent(eventData.EventId);
            }
        }

        public virtual void OnFailedEvent()
        {
            Debug.Log("OnFailedEvent");
            onEventFailed?.Invoke();

        }
    }
}