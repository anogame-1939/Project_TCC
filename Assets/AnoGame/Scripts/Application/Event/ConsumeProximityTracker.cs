using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnoGame.Application.Event
{
    [AddComponentMenu("Event/ConsumeProximityTracker")]
    public class ConsumeProximityTracker : MonoBehaviour
    {
        [SerializeField] private float _radius = 3f;
        [SerializeField] private LayerMask _zoneLayers;    // EventOnConsumeが乗るレイヤーだけに
        [SerializeField] private bool _logDebug = false;

        private readonly HashSet<IConsumeZone> _nearby = new();
        public IReadOnlyCollection<IConsumeZone> NearbyZones => _nearby;

        void Reset()
        {
            var col = gameObject.GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _radius;

            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        void OnValidate()
        {
            var col = GetComponent<SphereCollider>();
            if (col) col.radius = _radius;
        }

        void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _zoneLayers) == 0) return;

            // ゾーンは EventOnConsume 本体 or その子に置かれる想定
            var zone = other.GetComponentInParent<IConsumeZone>();
            if (zone != null && _nearby.Add(zone) && _logDebug)
                Debug.Log($"[ConsumeTracker] Enter: {zone.GetDebugName()}");
        }

        void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & _zoneLayers) == 0) return;
            var zone = other.GetComponentInParent<IConsumeZone>();
            if (zone != null && _nearby.Remove(zone) && _logDebug)
                Debug.Log($"[ConsumeTracker] Exit: {zone.GetDebugName()}");
        }

        /// もっとも適合するゾーンを返す（最初にOKなものを採用：必要なら優先度付けして拡張）
        public bool TryPickUsableZone(string itemId, GameObject user, Vector3 usePos,
                                    out IConsumeZone zone, out string reason)
        {
            foreach (var z in _nearby)
            {
                if (z.CanConsume(itemId, user, usePos, out reason))
                {
                    zone = z;
                    return true;
                }
            }
            zone = null;
            reason = "付近に使用可能なイベントがありません。";
            return false;
        }

        /// <summary>
        /// コンテキストメニューから呼んで、現在の NearbyZones をログに出す
        /// </summary>
        [ContextMenu("Debug/Print Nearby Zones")]
        private void DebugPrintNearbyZones()
        {
            if (_nearby.Count == 0)
            {
                Debug.Log("[ConsumeTracker] NearbyZones is empty", this);
                return;
            }

            var names = _nearby.Select(z => z.GetDebugName()).ToArray();
            Debug.Log($"[ConsumeTracker] NearbyZones ({_nearby.Count}): {string.Join(", ", names)}", this);
        }
    }
}