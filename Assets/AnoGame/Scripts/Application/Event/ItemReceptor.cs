using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Domain.Inventory; // For ItemConsumedArgs
using AnoGame.Application.Attributes; // For Custom Attributes
using System;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// Story 2用: アイテム使用を検知して、イベントサービスに「開始合図」を送るだけのセンサー
    /// </summary>
    [AddComponentMenu("AnoGame/Event/ItemReceptor")]
    public class ItemReceptor : MonoBehaviour
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

            // 3. 成功 -> イベントサービスへ開始要求を投げる
            Debug.Log($"[ItemReceptor] Hit! Request Event Start: {targetEventId}");
            if (!string.IsNullOrEmpty(targetEventId))
            {
                _eventService.TriggerEventStart(targetEventId);
            }
            else
            {
                Debug.LogWarning("[ItemReceptor] Target Event ID is not set.");
            }
        }

        private bool CheckProximity(ItemConsumedArgs args)
        {
            var userPos = args.User != null ? args.User.transform.position : Vector3.zero;
            // argsに座標が入っていない場合はUser座標、それもなければ諦める
            var usePos = args.UsePosition != default ? args.UsePosition : userPos;

            return Vector3.Distance(transform.position, usePos) <= maxDistance;
        }

        // デバッグ用可視化
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
