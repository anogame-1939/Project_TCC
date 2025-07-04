using UnityEngine;
using AnoGame.Data;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Event;
using VContainer;

namespace AnoGame.Application.Enemy
{
    public class EnemyEventController : MonoBehaviour
    {
        [SerializeField] private EnemyHitDetector hitDetector;
        [SerializeField] private EnemyLifespan lifespan;
        [SerializeField]
        private EventData _eventData;
        public EventData EventData => _eventData;

        private bool isChasing = false;
        public bool IsChasing => isChasing;

        [Inject] private IEventService _eventService;
        [Inject] private EventManager _eventManager;

        [Inject]
        public void Construct(
            IEventService eventService,
            EventManager eventManager
        )
        {
            _eventService = eventService;
            _eventManager = eventManager;
        }

        public void Initialize(EventData eventData)
        {
            _eventData = eventData;
            
            // EnemyLifespanのOnDestroyの前に判定を行うために
            isChasing = true;
            Debug.Log("セットアップ");
        }

        private void OnDisable()
        {
            Dispose();
        }

        // 時間切れ = 逃走成功
        public void HandleEscapeSuccess()
        {
            Debug.Log("HandleEscapeSuccess" + _eventData.EventName);
            if (_eventData != null)
            {
                // クリア済みのイベントIDを登録
                _eventManager.AddClearedEvent(_eventData.EventId);
                // イベントトリガーの実行
                _eventService.TriggerEventComplete(_eventData.EventId);
            }
        }

        // プレイヤーヒット = 逃走失敗
        private void HandleEscapeFail()
        {
            Debug.LogWarning("死");
            GameOverManager.Instance.OnGameOver();

            if (_eventData != null)
            {
                _eventService.TriggerEventFailed(_eventData.EventId);
            }
            
            Dispose();
        }

        private void Dispose()
        {
        }
    }
}