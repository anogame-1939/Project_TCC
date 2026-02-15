using System.Collections.Generic;
using AnoGame.AnoFlow;

namespace AnoGame.Application
{
    /// <summary>
    /// IEventStoreの実装。
    /// GameManagerのEventHistoryへの橋渡しを行う。
    /// </summary>
    public class GameEventStore : IEventStore
    {
        private readonly GameManager _gameManager;

        public GameEventStore(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void AddEvent(string eventId)
        {
            _gameManager.CurrentGameData.EventHistory.AddEvent(eventId);
        }

        public void AddTags(IEnumerable<string> tags)
        {
            _gameManager.CurrentGameData.EventHistory.AddTags(tags);
        }
    }
}
