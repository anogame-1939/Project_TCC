using UnityEngine;
using AnoGame.Infrastructure;
using AnoGame.Data;

namespace AnoGame.Application.Story.Manager
{
    public class StoryStateManager : SingletonMonoBehaviour<StoryStateManager>
    {
        private GameManager _gameManager;
        private StoryManager _storyManager;
        
        private void Start()
        {
            _gameManager = GameManager.Instance;
            _storyManager = StoryManager.Instance;
            
            // StoryManagerのチャプターロードイベントを購読
            _storyManager.ChapterLoaded += OnChapterLoaded;
            
            // GameManagerのセーブ/ロードイベントを購読
            // _gameManager.SaveGameData += OnSaveGameData;
            // _gameManager.LoadGameData += OnLoadGameData;
        }

        private void OnDestroy()
        {
            if (_storyManager != null)
            {
                _storyManager.ChapterLoaded -= OnChapterLoaded;
            }
            
            if (_gameManager != null)
            {
                _gameManager.SaveGameData -= OnSaveGameData;
                _gameManager.LoadGameData -= OnLoadGameData;
            }
        }

        private void OnChapterLoaded(bool useRetryPoint)
        {
            if (_gameManager == null || _storyManager == null) return;

            // 現在のストーリー進行状況をGameDataに反映
            UpdateGameDataProgress();
            
            // セーブポイントを作成（リトライポイントからのロードでない場合）
            if (!useRetryPoint)
            {
                CreateSavePoint();
            }
        }

        private void OnSaveGameData(GameData gameData)
        {
            if (_storyManager == null) return;
            
            // セーブ時に最新のストーリー進行状況を反映
            UpdateGameDataProgress();
        }

        private void OnLoadGameData(GameData gameData)
        {
            if (_storyManager == null) return;
            
            // StoryManagerの進行状況を更新
            _storyManager.UpdateGameData();
        }

        private void UpdateGameDataProgress()
        {
            var currentGameData = _gameManager.CurrentGameData;
            if (currentGameData == null) return;

            // 現在のストーリー進行状況を取得してGameDataに反映
            var progress = _storyManager.GetCurrentProgress();
            if (currentGameData.storyProgress == null)
            {
                currentGameData.storyProgress = new StoryProgress();
            }

            currentGameData.storyProgress.currentStoryIndex = progress.currentStoryIndex;
            currentGameData.storyProgress.currentChapterIndex = progress.currentChapterIndex;
            currentGameData.storyProgress.currentSceneIndex = progress.currentSceneIndex;

            // GameManagerに更新を通知
            _gameManager.UpdateGameState(currentGameData);
        }

        private async void CreateSavePoint()
        {
            bool success = await _gameManager.CreateSavePoint();
            if (!success)
            {
                Debug.LogWarning("Failed to create save point after chapter load");
            }
        }

        // 外部からストーリー進行状況の更新を強制するためのメソッド
        public void ForceUpdateProgress()
        {
            UpdateGameDataProgress();
        }
    }
}