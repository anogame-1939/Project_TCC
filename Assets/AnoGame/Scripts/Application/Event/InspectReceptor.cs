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

using AnoGame.Application;

namespace AnoGame.AnoFlow
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
        [HideInInspector]
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
            if (dist > maxDistance)
            {
                return float.NegativeInfinity;
            }

            return priority - dist;
        }

        public bool TryBuildOptions(Transform actor, List<InteractionOption> buffer)
        {
            float dist = Vector3.Distance(transform.position, actor.position);
            if (dist > maxDistance) return false;

            if (!CheckConditions()) return false;

            buffer.Add(new InteractionOption
            {
                Kind = InteractionKind.Inspect,
                Prompt = prompt,
                Priority = priority,
                RequiresHold = false,
                Execute = () => ExecuteEvent()
            });

            return true;
        }

        private void ExecuteEvent()
        {
            if (string.IsNullOrEmpty(targetEventId))
            {
                Debug.LogWarning($"[InspectReceptor] {gameObject.name}: targetEventId is null or empty!");
                return;
            }
            if (!CheckConditions())
            {
                return;
            }
            _eventService.TriggerEventStart(targetEventId);
            // ResultTags を付与
            if (eventData != null && eventData.ResultTags.Count > 0)
            {
                _eventService.AddTags(eventData.ResultTags);
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
                    if (!_inventoryService.HasItem(itemId)) return false;
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(targetEventId))
            {
                if (eventData != null && eventData.EventId == targetEventId) return;

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
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
