using AnoGame.Application.Core;

namespace AnoGame.Application.Event
{
    public class GameOverManager : SingletonMonoBehaviour<GameOverManager>
    {
        public void OnGameOver()
        {
            // データをリロード
            GameManager.Instance.ReloadData();

            // TODO:イベント公開する
        }
    }
}