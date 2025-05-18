using UnityEngine;
namespace AnoGame.Application.Event
{
    public class GameOverHandler : MonoBehaviour
    {
        public void GameOver()
        {
            GameOverManager.Instance.OnGameOver();

        }
    }
}