using UnityEngine;
using VContainer;
using AnoGame.Application.Player.Control;
using AnoGame.Apllication.Direction;

namespace AnoGame.Application.Direction
{
    [DisallowMultipleComponent]
    public class DirectionHandler : MonoBehaviour
    {
        [Inject] EventLockControl _eventLockControl;
        [Inject] CinematicBars _cinematicBars;
        [SerializeField] private GameStateHandler _gameStateHandler;


        [ContextMenu("StatEvent")]
        public void StatEvent()
        {
            _gameStateHandler.SetInGameEvent();
            _eventLockControl.BeginLock();
            _cinematicBars.Show();
        }

        [ContextMenu("EndEvent")]
        public void EndEvent()
        {
            _gameStateHandler.SetGameplay();
            _eventLockControl.EndLock();
            _cinematicBars.Hide();
        }
    }
}
