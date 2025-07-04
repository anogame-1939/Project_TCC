using UnityEngine;
using AnoGame.Application.Core;
using AnoGame.Domain.Data.Models;
using AnoGame.Application.Utils;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Story.Manager
{
    public class StoryStateManager : SingletonMonoBehaviour<StoryStateManager>
    {
        private GameManager2 _gameManager;
        private StoryManager _storyManager;
        
        private void Awake()
        {
            _gameManager = GameManager2.Instance;
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


            if (useRetryPoint)
            {

            }
            else
            {
                PlayerSpawnManager.Instance.SpawnPlayerAtStart();
                
            }
        }

        private void OnChapterLoaded(bool useRetryPoint)
        {
            if (_gameManager == null || _storyManager == null) return;


            if (useRetryPoint)
            {
                var gameData = GameManager2.Instance.CurrentGameData;
                var playerPosition = gameData.PlayerPosition;
                if (playerPosition != null)
                {
                }
            }
            else
            {
                // 現在のストーリー進行状況をGameDataに反映
                
            }
            // UpdateGameDataProgress();
        }

        private void OnLoadGameData(GameData gameData)
        {
            // if (_storyManager == null) return;
            
            // StoryManagerの進行状況を更新
            // _storyManager.UpdateGameData();

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
            Position3D position = new Position3D(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            Rotation3D rotation = new Rotation3D(player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w);
            currentGameData.UpdatePosition(position, rotation, "", "");

            Debug.Log($"位置情報を保存:{currentGameData.PlayerPosition.Position.X}, {currentGameData.PlayerPosition.Position.Y}, {currentGameData.PlayerPosition.Position.Z}");

            // GameManagerに更新を通知
            _gameManager.UpdateGameState(currentGameData);
        }

    }
}