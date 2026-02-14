using UnityEngine;
namespace AnoGame.AnoFlow
{
    public class GameOverHandler : MonoBehaviour
    {
        public void GameOver()
        {
            GameOverManager.Instance.OnGameOver();

        }
    }
}