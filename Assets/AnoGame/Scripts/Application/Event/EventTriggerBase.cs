using UnityEngine;
using UnityEngine.Events;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Event.Types;
using AnoGame.Data;
using AnoGame.Domain.Event.Conditions;
using System.Collections.Generic;
using System.Linq;

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
        [SerializeField] protected EventConditionComponent[] conditionComponents;

        private List<IEventCondition> _conditions = new List<IEventCondition>();

        // NOTE:重複登録防止。本来ならライフサイクルで制御したいところ
        private bool _registeredEvent = false;

        [Inject] protected IEventService _eventService;

        [Inject]
        public virtual void Construct(IEventService eventService)
        {
            _eventService = eventService;
            _eventService.LoadedClearEvent += InitializeEvents;

        }

        protected virtual void Start()
        {
            InitializeConditions();
            InitializeEvents();
        }

        protected virtual void InitializeEvents()
        {
            // クリア済みのイベント事項
            if (_eventService.IsEventCleared(eventData.EventId))
            {
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
                        observableCondition.OnConditionChanged += PrepareEvent;
                    }
                }
            }

            // 初期化のタイミングでチェック
            PrepareEvent();
        }

        /// <summary>
        /// TODO:未使用。どういう扱いにするのか後で考える。当初はアイテム持ってたらトリガーオブジェクトを有効にするとか考えてた。
        /// </summary>
        private void PrepareEvent()
        {
            if (CheckConditions())
            {
                onPrepareEvent?.Invoke();
            }
        }

        protected bool CheckConditions()
        {
            if (_conditions.Count == 0)
                return true;

            return _conditions.All(condition => condition.IsSatisfied());
        }

        protected virtual void OnDestroy()
        {
            foreach (var condition in _conditions)
            {
                if (condition is IObservableCondition observableCondition)
                {
                    observableCondition.OnConditionChanged -= PrepareEvent;
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
            // if (!CheckConditions())
                // return;

            onEventStart?.Invoke();
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
            Debug.Log("OnDoneEvent");
            onEventDone?.Invoke();

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