using UnityEngine;
using AnoGame.Application.Core;
using AnoGame.Application.Story;
using AnoGame.Application.Utils;

namespace AnoGame.Application
{
    public class PlayerPositionDataManager : SingletonMonoBehaviour<GameManager2>
    {
        [SerializeField]
        private GameObject playerObject;

        private GameManager2 _gameManager;
        private StoryManager _storyManager;
        
        private void Awake()
        {
            _gameManager = GameManager2.Instance;
            _storyManager = StoryManager.Instance;
            _storyManager.ChapterLoaded += OnChapterLoaded;
        }

        private void OnChapterLoaded(bool useRetryPoint)
        {
            if (_gameManager == null || _storyManager == null) return;

            if (useRetryPoint)
            {
                var gameData = _gameManager.CurrentGameData;
                var playerPosition = gameData.PlayerPosition.Position;
                // Position3D は struct なので null チェックは不要
            }
            else
            {
                SaveCurrentPlayerPosition();
                // UpdateGameDataProgress();
            }
        }

        private void SaveCurrentPlayerPosition()
        {
            if (playerObject == null || _gameManager == null) return;

            var transform = playerObject.transform;
            
            // Vector3Extensions を使用して変換
            var position = transform.position.ToPosition3D();
            var rotation = transform.rotation.ToRotation3D();

            // string currentMapId = _gameManager.CurrentMapId;
            // string currentAreaId = _gameManager.CurrentAreaId;

            string currentMapId = "";
            string currentAreaId = "";

            _gameManager.CurrentGameData.UpdatePosition(
                position,
                rotation,
                currentMapId,
                currentAreaId
            );
        }
    }
}