using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace AnoGame.AnoFlow
{
    /// <summary>
    /// クリア済みイベントの記録を管理する。
    /// IEventStore経由でゲームデータへの永続化を委譲する。
    /// </summary>
    public class EventManager
    {
        private readonly IEventStore _eventStore;

        [Inject]
        public EventManager(IEventStore eventStore)
        {
            Debug.Log("EventManager initialized");
            _eventStore = eventStore;
        }

        public void AddClearedEvent(string eventId)
        {
            _eventStore.AddEvent(eventId);
        }

        public void AddTags(IEnumerable<string> tags)
        {
            _eventStore.AddTags(tags);
        }
    }
}