using UnityEngine;
using VContainer;

namespace AnoGame.Application.Event
{
    /// <summary>
    /// EventManager呼び出してるけど、あまり意味ない
    /// _gameManager.CurrentGameData.EventHistoryも参照してるけど多分使ってない
    /// いや、使ってるわ
    /// ゲームデータとしてクリア済みイベントを保持させて、セーブ時に書き込んでる
    /// </summary>
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