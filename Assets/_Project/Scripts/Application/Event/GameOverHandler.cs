using UnityEngine;
namespace AnoGame.Application
{
    public class GameOverHandler : MonoBehaviour
    {
        public void GameOver()
        {
            GameOverManager.Instance.OnGameOver();

        }
    }
}