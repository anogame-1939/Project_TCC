using UnityEngine;
using UnityEngine.Events;
using VContainer;
using AnoGame.Domain.Event.Services;
using AnoGame.Application.Attributes;

namespace AnoGame.AnoFlow
{
    /// <summary>
    /// 特定イベントのクリアに反応する軽量コンポーネント。
    /// EventTriggerBase の代替として、クリア結果への反応と状態復元を担当する。
    ///
    /// 用途:
    /// - イベントクリアで複数のドアを開放
    /// - クリア後のNPC台詞変更
    /// - シーンロード時のクリア済み状態復元
    /// </summary>
    [AddComponentMenu("AnoGame/Event/EventReactor")]
    public class EventReactor : MonoBehaviour
    {
        [Header("監視対象")]
        [EventSelector]
        [SerializeField] private string watchEventId;

        [Header("イベント")]
        [Tooltip("イベントがクリアされた瞬間に実行される")]
        [SerializeField] private UnityEvent onEventCleared;

        [Tooltip("シーンロード時に既にクリア済みの場合に実行される（状態復元用）")]
        [SerializeField] private UnityEvent onAlreadyCleared;

        [Inject] private IEventService _eventService;

        private bool _hasReacted = false;

        [Inject]
        public void Construct(IEventService eventService)
        {
            _eventService = eventService;
            _eventService.LoadedClearEvent += OnClearEventsLoaded;
        }

        private void Start()
        {
            if (_eventService == null || string.IsNullOrEmpty(watchEventId))
                return;

            if (_eventService.IsEventCleared(watchEventId))
            {
                // 既にクリア済み → 状態復元
                _hasReacted = true;
                onAlreadyCleared?.Invoke();
            }
            else
            {
                // 未クリア → クリア完了を監視
                _eventService.RegisterCompleteEventHandler(watchEventId, OnWatchedEventCleared);
            }
        }

        /// <summary>
        /// セーブデータロード時のクリアイベント再読み込みに対応。
        /// </summary>
        private void OnClearEventsLoaded()
        {
            if (string.IsNullOrEmpty(watchEventId)) return;

            if (_eventService.IsEventCleared(watchEventId) && !_hasReacted)
            {
                _hasReacted = true;
                onAlreadyCleared?.Invoke();
            }
        }

        private void OnWatchedEventCleared()
        {
            if (_hasReacted) return;

            _hasReacted = true;
            onEventCleared?.Invoke();
        }

        private void OnDestroy()
        {
            if (_eventService != null)
            {
                _eventService.UnregisterCompleteEventHandler(watchEventId, OnWatchedEventCleared);
                _eventService.LoadedClearEvent -= OnClearEventsLoaded;
            }
        }
    }
}
