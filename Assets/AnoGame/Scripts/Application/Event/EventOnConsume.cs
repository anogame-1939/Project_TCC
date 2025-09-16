using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using AnoGame.Data;
using AnoGame.Application.Event;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;

[AddComponentMenu("Event/EventOnConsume")]
public class EventOnConsume : MonoBehaviour
{
    [Serializable]
    public class ConsumeRule
    {
        [Header("どのアイテムを消費したとき？")]
        public ItemData item;

        [Header("起動するイベント（EventData or 直接Trigger指定）")]
        public EventData eventData;
        public EventTriggerBase eventTriggerOverride; // 指定あればこちら優先

        [Header("実行条件")]
        public bool requireInsideCollider = false;
        public Collider areaCollider;   // Trigger推奨。未指定ならthisのColliderを探す
        public bool requireWithinDistance = false;
        public Transform distanceCenter; // 未指定なら this.transform
        public float maxDistance = 3f;
        public bool requireLineOfSight = false; // 任意：壁越し禁止などの時
        public LayerMask losBlockers = ~0;

        [Header("開始後の挙動")]
        public bool completeInstantly = false; // InstantEventTrigger不要で即クリアしたい場合
    }

    [SerializeField] private List<ConsumeRule> _rules = new();
    [SerializeField] private bool _logDebug = false;

    [Inject] private IInventoryService _inventory;
    [Inject] private IEventService _eventService;

    private void OnEnable()
    {
        if (_inventory != null)
            _inventory.OnItemConsumed += HandleItemConsumed;
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnItemConsumed -= HandleItemConsumed;
    }

    private void HandleItemConsumed(ItemConsumedArgs args)
    {
        foreach (var rule in _rules)
        {
            if (rule.item == null) continue;
            if (args.Item != rule.item) continue;

            if (!CheckProximity(rule, args)) continue;

            // 実行：Trigger優先、なければEventService
            var trigger = rule.eventTriggerOverride;
            if (trigger != null)
            {
                if (_logDebug) Debug.Log($"[EventOnConsume] Start via Trigger: {trigger.name}");
                trigger.StartEvent();
            }
            else if (rule.eventData != null)
            {
                if (_logDebug) Debug.Log($"[EventOnConsume] Start via EventService: {rule.eventData.EventId}");
                _eventService.TriggerEventStart(rule.eventData.EventId);
            }
            else
            {
                if (_logDebug) Debug.LogWarning("[EventOnConsume] ルールにイベント未指定");
                continue;
            }

            // 即クリア
            if (rule.completeInstantly)
            {
                var eventId = rule.eventTriggerOverride != null
                    ? rule.eventTriggerOverride.EventData.EventId
                    : rule.eventData.EventId;

                _eventService.TriggerEventComplete(eventId);
                // もし EventManager.AddClearedEvent を使うならここで呼ぶ
                // _eventManager.AddClearedEvent(eventId);
            }
        }
    }

    private bool CheckProximity(ConsumeRule rule, ItemConsumedArgs args)
    {
        var user = args.User != null ? args.User.transform : null;
        var refPos = args.UsePosition != default ? args.UsePosition
                    : user != null ? user.position
                    : transform.position;

        // コライダー内判定
        if (rule.requireInsideCollider)
        {
            var col = rule.areaCollider != null ? rule.areaCollider : GetComponent<Collider>();
            if (col == null)
            {
                if (_logDebug) Debug.LogWarning("[EventOnConsume] Collider未設定だが requireInsideCollider=true");
                return false;
            }

            // 厳密には Trigger進入/退出でフラグ管理が堅いが、簡易には Bounds 内判定
            if (!col.bounds.Contains(refPos))
                return false;
        }

        // 距離判定
        if (rule.requireWithinDistance)
        {
            var center = rule.distanceCenter != null ? rule.distanceCenter.position : transform.position;
            if (Vector3.Distance(center, refPos) > rule.maxDistance)
                return false;
        }

        // 視線判定（任意）
        if (rule.requireLineOfSight && args.User != null)
        {
            var center = rule.distanceCenter != null ? rule.distanceCenter.position : transform.position;
            var dir = center - args.User.transform.position;
            if (Physics.Raycast(args.User.transform.position, dir.normalized, out var hit, dir.magnitude, rule.losBlockers))
            {
                // 何かに遮られていればNG
                return false;
            }
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_rules == null) return;
        foreach (var r in _rules)
        {
            if (r.requireWithinDistance)
            {
                var c = r.distanceCenter != null ? r.distanceCenter.position : transform.position;
                Gizmos.DrawWireSphere(c, r.maxDistance);
            }
        }
    }
#endif
}
