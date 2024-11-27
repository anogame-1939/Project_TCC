using UnityEngine;
using AnoGame.Application.Story;

namespace AnoGame.Application.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        private EnemySpawnManager _spawnManager;

        private void Awake()
        {
            _spawnManager = EnemySpawnManager.Instance;
            if (_spawnManager == null)
            {
                Debug.LogError("EnemySpawnManagerが見つかりません。");
            }
        }

        /// <summary>
        /// このスポナーの位置の近くに敵をスポーンさせる
        /// </summary>
        public void TriggerEnemySpawn()
        {
            if (_spawnManager != null)
            {
                _spawnManager.SpawnEnemyNearPlayer(transform.position);
            }
            else
            {
                Debug.LogError("EnemySpawnManagerの参照が見つかりません。");
            }
        }

        /// <summary>
        /// 指定した位置の近くに敵をスポーンさせる
        /// </summary>
        public void TriggerEnemySpawnAtPosition(Vector3 position)
        {
            if (_spawnManager != null)
            {
                _spawnManager.SpawnEnemyNearPlayer(position);
            }
            else
            {
                Debug.LogError("EnemySpawnManagerの参照が見つかりません。");
            }
        }

        /// <summary>
        /// スポーンした敵の移動を開始させる
        /// </summary>
        public void StartEnemyMovement()
        {
            if (_spawnManager != null)
            {
                _spawnManager.StartEnemyMovement();
            }
        }

        /// <summary>
        /// スポーンした敵の移動を停止させる
        /// </summary>
        public void StopEnemyMovement()
        {
            if (_spawnManager != null)
            {
                _spawnManager.StopEnemyMovement();
            }
        }
    }
}