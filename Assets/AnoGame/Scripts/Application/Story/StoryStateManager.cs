using UnityEngine;
using AnoGame.Application.Core;
using AnoGame.Data;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Story.Manager
{
    public class StoryStateManager : SingletonMonoBehaviour<StoryStateManager>
    {
        private GameManager _gameManager;
        private StoryManager _storyManager;
        
        private void Awake()
        {
            _gameManager = GameManager.Instance;
            _storyManager = StoryManager.Instance;
            
            // StoryManagerのチャプターロードイベントを購読
            _storyManager.ChapterLoaded += OnChapterLoaded;
            
            // GameManagerのセーブ/ロードイベントを購読
            // _gameManager.SaveGameData += OnSaveGameData;
            _gameManager.LoadGameData += OnLoadGameData;
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


            if (useRetryPoint)
            {
                var gameData = GameManager.Instance.CurrentGameData;
                var playerPosition = gameData.playerPosition;
                if (playerPosition != null)
                {
                    if (playerPosition.IsPositionValid)
                    {
                        // いらない？？
                        // SpawnPlayer(playerPosition);
                    }
                }
            }
            else
            {
                // 現在のストーリー進行状況をGameDataに反映
                UpdateGameDataProgress();
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

            var playerPosition = gameData.playerPosition;
            if (playerPosition != null)
            {
                if (playerPosition.IsPositionValid)
                {
                    SpawnPlayer(playerPosition);
                }
            }
        }

        private void SpawnPlayer(PlayerPositionData playerPosition)
        {
            // プレイヤーを前回終了位置に配置
            var player = GameObject.FindGameObjectWithTag(SLFBRules.TAG_PLAYER);
            if (player != null)
            {
                var brain = player.GetComponent<CharacterBrain>();
                brain.Warp(playerPosition.position.ToVector3(), playerPosition.rotation.ToQuaternion());
            }
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

            // ここで位置情報を取得して保存
            if (currentGameData.playerPosition == null)
            {
                currentGameData.playerPosition = new PlayerPositionData();
            }

            var player = GameObject.FindGameObjectWithTag(SLFBRules.TAG_PLAYER);
            if (player != null)
            {
                currentGameData.playerPosition.position = new Vector3SerializableData(player.transform.position);
                currentGameData.playerPosition.rotation = new QuaternionSerializableData(player.transform.rotation);
            }

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