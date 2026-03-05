using AnoGame.Data;
using UnityEngine;

namespace AnoGame.Messages
{
    // 取得を試みる要求（誰が / 何を）
    public struct TryCollectItemRequest
    {
        public Transform Picker;          // 拾った側（プレイヤー）
        public CollectableItem Source;    // 現場の CollectableItem
        public TryCollectItemRequest(Transform picker, CollectableItem source)
        { Picker = picker; Source = source; }
    }

    // 取得成功
    public struct ItemCollected
    {
        public ItemData ItemData;
        public int Quantity;
        public string UniqueId;           // インスタンス識別
        public Transform Picker;
        public ItemCollected(ItemData data, int qty, string uid, Transform picker)
        { ItemData = data; Quantity = qty; UniqueId = uid; Picker = picker; }
    }

    // 取得失敗（任意：満杯などの理由を付与）
    public enum CollectFailReason { InventoryFull, Invalid, Other }
    public struct ItemCollectFailed
    {
        public CollectFailReason Reason;
        public CollectableItem Source;
        public Transform Picker;
        public ItemCollectFailed(CollectFailReason r, CollectableItem s, Transform p)
        { Reason = r; Source = s; Picker = p; }
    }
}
