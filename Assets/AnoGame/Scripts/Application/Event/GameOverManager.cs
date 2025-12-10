using System;
using System.Linq;
using AnoGame.Application.Core;
using AnoGame.Application.Enemy;
using AnoGame.Application.Steam;
using AnoGame.Application.Story;
using AnoGame.Domain.Event.Services;
using AnoGame.Domain.Inventory.Services;
using UniRx;
using VContainer;


namespace AnoGame.Application.Event
{
    public class GameOverManager : SingletonMonoBehaviour<GameOverManager>
    {
        public event Action GameOver;



        [Button]
        public void OnGameOver()
        {
            UnityEngine.Debug.Log("OnGameOver");

            GameStateManager.Instance.SetState(GameState.GameOver);
            // EnemySpawnManager.Instance.DestroyCurrentEnemyInstance();
            if (EnemySpawnManager.Instance.CurrentEnemyInstance != null)
            {
                Destroy(EnemySpawnManager.Instance.CurrentEnemyInstance);
            }

            // 主にゲームオーバー画面表示で使用
            GameOver?.Invoke();

            // ここでリトライシーンを読み込む
            ReloadData();
        }

        private async void ReloadData()
        {
            // ストーリー進行リセット処理呼び出し
            StoryManager.Instance.ResetStoryProgress();
        }

        public async void OnRetryGame()
        {
            GameStateManager.Instance.SetState(GameState.Gameplay);
            MessageBroker.Default.Publish(new PlayerRetriedEvent());

            // シーンをリロード
            StoryManager.Instance.ReloadStoryScene();
        }
    }
}