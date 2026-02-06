using System.Collections.Generic;
using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Attributes;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.Player; // InteractionController

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

        [Header("Interaction Settings")]
        [SerializeField] private string prompt = "調べる";
        [SerializeField] private float maxDistance = 2.0f;
        [SerializeField] private int priority = 100;

        [Header("Reference")]
        [SerializeField] private Transform uiAnchor;

        [Inject] private IEventService _eventService;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
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
                _eventService.TriggerEventStart(targetEventId);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
