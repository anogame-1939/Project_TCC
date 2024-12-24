using UnityEngine;
using AnoGame.Data;
using AnoGame.Domain.Event.Services;
using VContainer;

namespace AnoGame.Application.Enemy
{
    public class EnemyEventController : MonoBehaviour
    {
        [SerializeField] private EnemyHitDetector hitDetector;
        [SerializeField] private EnemyLifespan lifespan;

        private EventData _eventData;
        [Inject] private IEventService _eventService;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
        }

        public void Initialize(EventData eventData)
        {
            _eventData = eventData;
            
            // EnemyLifespanのOnDestroyの前に判定を行うために
            lifespan.OnLifespanExpired += HandleEscapeSuccess;
            hitDetector.OnPlayerHit += HandleEscapeFail;
        }

        private void OnDisable()
        {
            if (lifespan != null)
            {
                lifespan.OnLifespanExpired -= HandleEscapeSuccess;
            }
            if (hitDetector != null)
            {
                hitDetector.OnPlayerHit -= HandleEscapeFail;
            }
        }

        // 時間切れ = 逃走成功
        private void HandleEscapeSuccess()
        {
            if (_eventData != null)
            {
                _eventService.TriggerEventComplete(_eventData.EventId);
            }
        }

        // プレイヤーヒット = 逃走失敗
        private void HandleEscapeFail()
        {
            if (_eventData != null)
            {
                _eventService.TriggerEventFailed(_eventData.EventId);
            }
        }
    }
}