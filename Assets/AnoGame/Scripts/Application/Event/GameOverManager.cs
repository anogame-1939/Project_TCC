using System;
using UnityEngine;
using AnoGame.Application.Core;
using AnoGame.Application.Enemy;
using AnoGame.Application.Steam;
using AnoGame.Application.Story;
using UniRx;
using AnoGame.Domain.Event;


namespace AnoGame.Application.Event
{
    public class GameOverManager : SingletonMonoBehaviour<GameOverManager>
    {
        public event Action GameOver;

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

#if UNITY_EDITOR
        [Button]
        public void DebugPlayerDeath()
        {
            MessageBroker.Default.Publish(new PlayerDeathEvent(0, 2));
        }
#endif
    }
}