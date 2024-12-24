using AnoGame.Infrastructure;
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