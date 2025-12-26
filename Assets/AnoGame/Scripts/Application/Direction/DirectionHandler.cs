using UnityEngine;
using VContainer;
using AnoGame.Application.Player.Control;
using AnoGame.Application.Enemy;
using AnoGame.Apllication.Direction;

namespace AnoGame.Application.Direction
{
    [DisallowMultipleComponent]
    public class DirectionHandler : MonoBehaviour
    {
        [Inject] EventLockControl _eventLockControl;
        [Inject] CinematicBars _cinematicBars;
        [Inject] EnemySpawnManager _enemySpawnManager;
        [SerializeField] private GameStateHandler _gameStateHandler;


        [ContextMenu("StatEvent")]
        public void StatEvent()
        {
            _gameStateHandler.SetInGameEvent();
            _eventLockControl.BeginLock();

            if (_enemySpawnManager.CurrentEnemyInstance != null)
            {
                if (_enemySpawnManager.CurrentEnemyInstance.TryGetComponent<EventLockControl>(out var enemyEventLock))
                {
                    enemyEventLock.BeginLock();
                }
            }

            _cinematicBars.Show();
        }

        [ContextMenu("EndEvent")]
        public void EndEvent()
        {
            _gameStateHandler.SetGameplay();
            _eventLockControl.EndLock();

            if (_enemySpawnManager.CurrentEnemyInstance != null)
            {
                if (_enemySpawnManager.CurrentEnemyInstance.TryGetComponent<EventLockControl>(out var enemyEventLock))
                {
                    enemyEventLock.EndLock();
                }
            }

            _cinematicBars.Hide();
        }
    }
}
