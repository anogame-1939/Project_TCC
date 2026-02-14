using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using AnoGame.Application.Attributes;
using AnoGame.Data;
using System.Collections;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// 指定距離に近づいたらイベントを実行する（Collider不要）
    /// </summary>
    [AddComponentMenu("AnoGame/Event/ContactReceptor")]
    public class ContactReceptor : MonoBehaviour
    {
        [Header("Event")]
        [EventSelector]
        [SerializeField] private string targetEventId;
        [HideInInspector]
        [SerializeField] private EventData eventData;

        [Header("Settings")]
        [SerializeField] private float triggerDistance = 2.0f;
        [SerializeField] private bool once = true;

        [Inject] private IEventService _eventService;
        [Inject] private IInventoryService _inventoryService;
        private Transform _playerTransform;
        private bool _isTriggered = false;
        private static readonly float CHECK_INTERVAL = 0.2f;

        [Inject]
        public void Construct(IEventService eventService, IInventoryService inventoryService)
        {
            _eventService = eventService;
            _inventoryService = inventoryService;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _playerTransform = p.transform;

            StartCoroutine(CheckDistanceRoutine());
        }

        private IEnumerator CheckDistanceRoutine()
        {
            var wait = new WaitForSeconds(CHECK_INTERVAL);

            while (true)
            {
                if (once && _isTriggered) yield break;
                if (_playerTransform == null)
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) _playerTransform = p.transform;
                    yield return wait;
                    continue;
                }

                if (Vector3.Distance(transform.position, _playerTransform.position) <= triggerDistance)
                {
                    ExecuteEvent();
                }

                yield return wait;
            }
        }

        private void ExecuteEvent()
        {
            if (once && _isTriggered) return;
            if (!CheckConditions()) return;

            _isTriggered = true;

            Debug.Log($"[ContactReceptor] Triggered: {targetEventId}");
            if (!string.IsNullOrEmpty(targetEventId))
            {
                _eventService.TriggerEventStart(targetEventId);
                // ResultTags を付与
                if (eventData != null && eventData.ResultTags.Count > 0)
                {
                    _eventService.AddTags(eventData.ResultTags);
                }
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

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
    }
}
