using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Inventory;
using AnoGame.Application.Attributes;
using AnoGame.Data;
using System;

namespace AnoGame.AnoFlow
{
    /// <summary>
    /// Story 2用: アイテム使用を検知して、イベントサービスに「開始合図」を送るだけのセンサー
    /// </summary>
    [AddComponentMenu("AnoGame/Event/ItemReceptor")]
    public class ItemReceptor : MonoBehaviour, IConsumeZone
    {
        [Header("検知設定")]
        [Tooltip("このアイテムが使われたら反応する")]
        [ItemSelector]
        public string targetItemId;

        [Tooltip("反応した時に開始させるイベントID")]
        [EventSelector]
        public string targetEventId;

        [Tooltip("プレイヤーとの最大距離")]
        public float maxDistance = 3.0f;

        [Header("条件データ")]
        [HideInInspector]
        [SerializeField] private EventData eventData;

        [HideInInspector]
        [SerializeField] private ItemData itemData; // targetItemIdから自動解決

        // 依存性注入
        [Inject] private IInventoryService _inventory;
        [Inject] private IEventService _eventService;

        private bool _subscribed = false;

        [Inject]
        public void Construct(IInventoryService inventory, IEventService eventService)
        {
            _inventory = inventory;
            _eventService = eventService;
        }

        private void Start()
        {
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
            // 1. アイテムIDチェック
            if (!string.Equals(args.ItemId, targetItemId, StringComparison.Ordinal)) return;

            // 2. 距離チェック
            if (!CheckProximity(args))
            {
                Debug.Log($"[ItemReceptor] {targetItemId} was used but too far.");
                return;
            }

            // 3. 条件チェック
            if (!CheckConditions())
            {
                Debug.Log($"[ItemReceptor] {targetEventId} conditions not met.");
                return;
            }

            // 4. 成功 -> イベントサービスへ開始要求を投げる
            Debug.Log($"[ItemReceptor] Hit! Request Event Start: {targetEventId}");
            if (!string.IsNullOrEmpty(targetEventId))
            {
                _eventService.TriggerEventStart(targetEventId);
                // ResultTags を付与
                if (eventData != null && eventData.ResultTags.Count > 0)
                {
                    _eventService.AddTags(eventData.ResultTags);
                }
            }
            else
            {
                Debug.LogWarning("[ItemReceptor] Target Event ID is not set.");
            }
        }

        private bool CheckConditions()
        {
            // 自分自身のイベントがクリア済みなら再実行不可
            if (!string.IsNullOrEmpty(targetEventId) && _eventService.IsEventCleared(targetEventId))
                return false;

            if (eventData == null) return true;

            var items = eventData.RequiredItemIds;
            var events = eventData.RequiredEventIds;

            if (items != null)
            {
                foreach (var itemId in items)
                {
                    if (string.IsNullOrEmpty(itemId)) continue;
                    if (!_inventory.HasItem(itemId)) return false;
                }
            }

            if (events != null)
            {
                foreach (var evtId in events)
                {
                    if (string.IsNullOrEmpty(evtId)) continue;
                    if (!_eventService.IsEventCleared(evtId)) return false;
                }
            }

            // ConditionTags チェック
            var tags = eventData.ConditionTags;
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    if (!_eventService.HasTag(tag)) return false;
                }
            }

            return true;
        }

        private bool CheckProximity(ItemConsumedArgs args)
        {
            var userPos = args.User != null ? args.User.transform.position : Vector3.zero;
            var usePos = args.UsePosition != default ? args.UsePosition : userPos;

            return Vector3.Distance(transform.position, usePos) <= maxDistance;
        }

        // ── IConsumeZone 実装 ──────────────────────────────

        public bool CanConsume(string itemId, GameObject user, Vector3 usePos, out string reason)
        {
            reason = null;

            // アイテムIDチェック: 安定ID (targetItemId) と表示名 (ItemData.ItemName) の両方でマッチング
            bool idMatch = string.Equals(itemId, targetItemId, StringComparison.Ordinal);
            if (!idMatch && itemData != null)
            {
                idMatch = string.Equals(itemId, itemData.ItemName, StringComparison.Ordinal);
            }
            if (!idMatch)
            {
                reason = "このアイテムではここで実行できるイベントがありません。";
                return false;
            }

            // 距離チェック（既存ロジック再利用）
            var args = new ItemConsumedArgs(itemId, 1, user, usePos);
            if (!CheckProximity(args))
            {
                reason = "距離が遠すぎます。近づいてから使用してください。";
                return false;
            }

            // 条件チェック（既存ロジック再利用）
            if (!CheckConditions())
            {
                reason = "条件が満たされていません。";
                return false;
            }

            return true;
        }

        public bool TryStart(string itemId, GameObject user, Vector3 usePos)
        {
            if (!CanConsume(itemId, user, usePos, out _)) return false;

            if (!string.IsNullOrEmpty(targetEventId))
            {
                Debug.Log($"[ItemReceptor] IConsumeZone.TryStart: {targetEventId}");
                _eventService.TriggerEventStart(targetEventId);
                // ResultTags を付与
                if (eventData != null && eventData.ResultTags.Count > 0)
                {
                    _eventService.AddTags(eventData.ResultTags);
                }
                return true;
            }

            Debug.LogWarning("[ItemReceptor] Target Event ID is not set.");
            return false;
        }

        public string GetDebugName() => $"ItemReceptor({name}:{targetItemId})";

#if UNITY_EDITOR
        private void OnValidate()
        {
            // EventData の自動解決
            if (!string.IsNullOrEmpty(targetEventId))
            {
                if (eventData == null || eventData.EventId != targetEventId)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EventData");
                    foreach (var guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EventData>(path);
                        if (asset != null && asset.EventId == targetEventId)
                        {
                            eventData = asset;
                            UnityEditor.EditorUtility.SetDirty(this);
                            break;
                        }
                    }
                }
            }

            // ItemData の自動解決（targetItemId → ItemData）
            if (!string.IsNullOrEmpty(targetItemId))
            {
                if (itemData == null || itemData.ItemId != targetItemId)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
                    foreach (var guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                        if (asset != null && asset.ItemId == targetItemId)
                        {
                            itemData = asset;
                            UnityEditor.EditorUtility.SetDirty(this);
                            break;
                        }
                    }
                }
            }
        }
#endif

        // デバッグ用可視化
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
