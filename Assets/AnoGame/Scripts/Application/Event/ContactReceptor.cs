using UnityEngine;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Attributes; // EventSelector if available
using AnoGame.Application.Player.Interaction; // For distance/trigger logic if needed, but this is auto-trigger
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

        [Header("Settings")]
        [SerializeField] private float triggerDistance = 2.0f;
        [SerializeField] private bool once = true;

        [Inject] private IEventService _eventService;
        private Transform _playerTransform;
        private bool _isTriggered = false;
        private static readonly float CHECK_INTERVAL = 0.2f;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
        }

        private void Start()
        {
            // Player検索 (Tag or DI) - ここでは簡易的にTag検索
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
                    // 再検索
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
            _isTriggered = true;

            Debug.Log($"[ContactReceptor] Triggered: {targetEventId}");
            if (!string.IsNullOrEmpty(targetEventId))
            {
                _eventService.TriggerEventStart(targetEventId);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
    }
}
