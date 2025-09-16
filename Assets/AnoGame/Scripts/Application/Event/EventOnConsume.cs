using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using AnoGame.Data;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;

namespace AnoGame.Application.Event
{
    [AddComponentMenu("Event/EventOnConsume")]
    public class EventOnConsume : MonoBehaviour, IConsumeZone
    {
        [Serializable]
        public class ConsumeRule
        {
            [Header("どのアイテムを消費したとき？")]
            public ItemData item;          // Inspector 用（編集体験）
            [SerializeField, Tooltip("実行時に使う安定ID。OnValidateでitemから自動反映")]
            public string itemId;          // 実行時はこちらのみ使用

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

        private bool _subscribed;

        public bool CanConsume(string itemId, GameObject user, Vector3 usePos, out string reason)
            => CanConsumeNow(itemId, user, usePos, out reason);

        public bool TryStart(string itemId, GameObject user, Vector3 usePos)
        {
            // 「今使えるか」をもう一度チェックしてから、既存の起動経路と同じ処理へ
            if (!CanConsume(itemId, user, usePos, out _)) return false;

            var args = new ItemConsumedArgs(itemId, 1, user, usePos);
            HandleItemConsumed(args); // ← 既存のルール走査で Start/Complete される
            return true;
        }

        public string GetDebugName() => name;

        public bool CanConsumeNow(string itemId, GameObject user, Vector3 usePos, out string reason)
        {
            reason = null;
            var anyRule = false;
            foreach (var rule in _rules)
            {
                if (string.IsNullOrEmpty(rule.itemId)) continue;
                if (!string.Equals(rule.itemId, itemId, StringComparison.Ordinal)) continue;
                anyRule = true;

                var args = new ItemConsumedArgs(itemId, 1, user, usePos);
                if (CheckProximity(rule, args)) return true;
            }

            reason = anyRule
                ? "距離が遠いか、遮蔽物があります。近づいてから使用してください。"
                : "このアイテムではここで実行できるイベントがありません。";
            return false;
        }

        private void Start()
        {
            // VContainerの注入順対策：Startで購読開始
            if (!_subscribed && _inventory != null)
            {
                _inventory.OnItemConsumed += HandleItemConsumed;
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_subscribed && _inventory != null)
            {
                _inventory.OnItemConsumed -= HandleItemConsumed;
                _subscribed = false;
            }
        }

        private void HandleItemConsumed(ItemConsumedArgs args)
        {
            foreach (var rule in _rules)
            {
                // ← ここを ItemId 比較に変更
                if (string.IsNullOrEmpty(rule.itemId)) continue;
                if (!string.Equals(args.ItemId, rule.itemId, StringComparison.Ordinal)) continue;

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
                    // 必要なら EventManager.AddClearedEvent(eventId);
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

                // 厳密：Triggerで入退場フラグ管理が安全。簡易：Bounds内判定
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
                if (Physics.Raycast(args.User.transform.position, dir.normalized, out _, dir.magnitude, rule.losBlockers))
                {
                    // 何かに遮られていればNG
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspector 快適化：ItemData から itemId に自動反映
            if (_rules == null) return;
            foreach (var r in _rules)
            {
                if (r.item != null)
                {
                    // 【前提】ItemData に ItemId がある
                    r.itemId = r.item.ItemId; // 無ければ r.item.ItemName に置換
                }
            }
        }

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
#else
        private void OnDrawGizmosSelected() { }
#endif
    }
}