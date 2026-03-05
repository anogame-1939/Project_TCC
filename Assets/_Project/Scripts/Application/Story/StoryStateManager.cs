using UnityEngine;
using System.Collections.Generic;
using AnoGame.Application.Core;
using AnoGame.Domain.Data.Models;
using AnoGame.Application.Utils;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Story.Manager
{
    public class StoryStateManager : SingletonMonoBehaviour<StoryStateManager>
    {
        [System.Serializable]
        public class StorySpawnData
        {
            public int StoryIndex;
            public List<Transform> ChapterSpawnPoints = new List<Transform>();
        }

        [SerializeField]
        private List<StorySpawnData> _storySpawnDataList = new List<StorySpawnData>();

        private GameManager _gameManager;
        private StoryManager _storyManager;

        protected override void Awake()
        {
            base.Awake();
            _gameManager = GameManager.Instance;
            _storyManager = StoryManager.Instance;

            // StoryManagerのチャプターロードイベントを購読
            _storyManager.StoryLoaded += OnStoryLoaded;
            _storyManager.ChapterLoaded += OnChapterLoaded;

            // GameManagerのセーブ/ロードイベントを購読
            // _gameManager.SaveGameData += OnSaveGameData;
            _gameManager.LoadGameData += OnLoadGameData;
        }

        private void OnDestroy()
        {
            if (_storyManager != null)
            {
                _storyManager.StoryLoaded -= OnStoryLoaded;
                _storyManager.ChapterLoaded -= OnChapterLoaded;
            }

            if (_gameManager != null)
            {
                // _gameManager.SaveGameData -= OnSaveGameData;
                _gameManager.LoadGameData -= OnLoadGameData;
            }
        }

        private void OnStoryLoaded(bool useRetryPoint)
        {
            if (_gameManager == null || _storyManager == null) return;

            // StoryLoaded時点ではシーンロード前の場合があるため、ここでのスポーンは行わない
            if (useRetryPoint)
            {

            }
            else
            {
                // PlayerSpawnManager.Instance.SpawnPlayerAtStart();
            }
        }

        private void OnChapterLoaded(bool useRetryPoint)
        {
            if (_gameManager == null || _storyManager == null) return;

            // シーンロード完了後に呼ばれる

            if (useRetryPoint)
            {
                var gameData = GameManager.Instance.CurrentGameData;
                var playerPosition = gameData.PlayerPosition;

                // ここでリトライポイントへのスポーンなどの処理が必要なら追加
                PlayerSpawnManager.Instance.SpawnPlayerAtRetryPoint();
            }
            else
            {
                Debug.Log("Spawning at default StartPoint.");
                // ストーリーごとの初期スポーン位置を取得してスポーン
                var progress = _storyManager.GetCurrentProgress();
                Transform spawnPoint = GetSpawnPoint(progress.CurrentStoryIndex, progress.CurrentChapterIndex);

                if (spawnPoint != null)
                {
                    Debug.Log($"Spawning at Story {progress.CurrentStoryIndex}, Chapter {progress.CurrentChapterIndex} defined point: {spawnPoint.name}");
                    PlayerSpawnManager.Instance.SpawnToPosition(spawnPoint);
                }
                else
                {
                    Debug.Log("No specific spawn point found, spawning at default StartPoint.");
                    // PlayerSpawnManager.Instance.SpawnPlayerAtStart();
                }
            }
            // UpdateGameDataProgress();
        }

        private Transform GetSpawnPoint(int storyIndex, int chapterIndex)
        {
            var storyData = _storySpawnDataList.Find(x => x.StoryIndex == storyIndex);
            if (storyData != null)
            {
                if (chapterIndex >= 0 && chapterIndex < storyData.ChapterSpawnPoints.Count)
                {
                    return storyData.ChapterSpawnPoints[chapterIndex];
                }
            }
            return null;
        }

        private void OnLoadGameData(GameData gameData)
        {
            // if (_storyManager == null) return;

            // StoryManagerの進行状況を更新
            // _storyManager.UpdateGameData();

            // ゲームロード時はセーブデータ位置を優先する場合が多いが、
            // 状況によるため要確認。ここでは一旦既存ロジック通り
            var playerPosition = gameData.PlayerPosition;
            if (playerPosition != null)
            {
                SpawnPlayer(playerPosition);
            }
        }

        private void SpawnPlayer(PlayerPosition playerPosition)
        {
            // プレイヤーを前回終了位置に配置
            var player = GameObject.FindGameObjectWithTag(AnoGame.Data.SLFBRules.TAG_PLAYER);
            if (player != null)
            {
                var brain = player.GetComponent<CharacterBrain>();
                brain.Warp(playerPosition.Position.ToVector3(), playerPosition.Rotation.ToQuaternion());
            }
        }

        public void UpdatePlayerPosition()
        {
            var currentGameData = _gameManager.CurrentGameData;
            if (currentGameData == null) return;

            // 位置情報を取得して保存
            var player = GameObject.FindGameObjectWithTag(AnoGame.Data.SLFBRules.TAG_PLAYER);
            if (player == null) return;

            Position3D position = new Position3D(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            Rotation3D rotation = new Rotation3D(player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w);
            currentGameData.UpdatePosition(position, rotation, "", "");

            Debug.Log($"位置情報を保存:{currentGameData.PlayerPosition.Position.X}, {currentGameData.PlayerPosition.Position.Y}, {currentGameData.PlayerPosition.Position.Z}");

            // GameManagerに更新を通知
            _gameManager.UpdateGameState(currentGameData);
        }

    }
}