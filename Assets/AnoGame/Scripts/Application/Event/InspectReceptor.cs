using System.Collections.Generic;
using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Attributes;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.Player; // InteractionController
using AnoGame.Domain.Inventory.Services;
using AnoGame.Data;
using AnoGame.Domain.Event.Conditions;
using System.Linq;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// 調べるイベント（Collider不要、InteractionControllerに登録）
    /// </summary>
    [AddComponentMenu("AnoGame/Event/InspectReceptor")]
    public class InspectReceptor : MonoBehaviour, IInteractable
    {
        [Header("Event")]
        [EventSelector]
        [SerializeField] private string targetEventId;
        [SerializeField] private EventData eventData; // V2: Direct reference for conditions

        [Header("Interaction Settings")]
        [SerializeField] private string prompt = "調べる";
        [SerializeField] private float maxDistance = 2.0f;
        [SerializeField] private int priority = 100;

        [Header("Reference")]
        [SerializeField] private Transform uiAnchor;

        [Inject] private IEventService _eventService;
        [Inject] private IInventoryService _inventoryService;

        [Inject]
        public void Construct(IEventService eventService, IInventoryService inventoryService)
        {
            _eventService = eventService;
            _inventoryService = inventoryService;
        }

        private void OnEnable()
        {
            // Colliderを使わないため、手動で登録
            InteractionController.RegisterManual(this);
        }

        private void OnDisable()
        {
            InteractionController.UnregisterManual(this);
        }

        // --- IInteractable Implementation ---

        public Vector3 WorldUiAnchor => uiAnchor != null ? uiAnchor.position : transform.position + Vector3.up * 1.5f;

        public float Score(Transform actor)
        {
            float dist = Vector3.Distance(transform.position, actor.position);
            if (dist > maxDistance) return float.NegativeInfinity;

            // 近いほど優先
            return priority - dist;
        }

        public bool TryBuildOptions(Transform actor, List<InteractionOption> buffer)
        {
            // 距離チェックは Score で行われているが、念のため
            if (Vector3.Distance(transform.position, actor.position) > maxDistance) return false;

            // Check conditions before showing option? Or show but locked?
            // Usually "Inspect" options might be hidden if conditions not met, or shown.
            // For now, let's consistency check: if conditions not met, maybe don't show or show different prompt?
            // User requirement: "Condition check logic ... efficient ... accessible".
            // If we hide it, we need to check conditions here.
            if (!CheckConditions()) return false;

            buffer.Add(new InteractionOption
            {
                Kind = InteractionKind.Inspect,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = false, // タップで即実行ならfalse, 長押しならtrue
                Execute = () => ExecuteEvent()
            });

            return true;
        }

        private void ExecuteEvent()
        {
            Debug.Log($"[InspectReceptor] Inspected: {targetEventId}");
            if (!string.IsNullOrEmpty(targetEventId))
            {
                if (CheckConditions())
                {
                    _eventService.TriggerEventStart(targetEventId);
                }
            }
        }

        private bool CheckConditions()
        {
            if (eventData == null) return true; // No data, assume no conditions or legacy behavior (allow)

            // Normalize
            var items = eventData.RequiredItemIds;
            var events = eventData.RequiredEventIds;

            if (items != null)
            {
                foreach (var itemId in items)
                {
                    if (string.IsNullOrEmpty(itemId)) continue;
                    // Check inventory
                    if (!_inventoryService.HasItem(itemId)) return false;
                }
            }

            if (events != null)
            {
                foreach (var evtId in events)
                {
                    if (string.IsNullOrEmpty(evtId)) continue;
                    // Check event history
                    if (!_eventService.IsEventCleared(evtId)) return false;
                }
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
