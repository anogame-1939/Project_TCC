using UnityEngine;
using VContainer;

namespace AnoGame.Application.Event
{
    public class EventManager
    {
        private readonly GameManager2 _gameManager;

        [Inject]
        public EventManager(GameManager2 gameManager)
        {
            Debug.Log("EventManager initialized");
            _gameManager = gameManager;
        }

        public void AddClearedEvent(string eventId)
        {
            _gameManager.CurrentGameData.EventHistory.AddEvent(eventId);
        }





    }
}