using UnityEngine;
using AnoGame.Application.Story;
using System.Collections;
using AnoGame.Data;

namespace AnoGame.Application.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField]
        EventData _eventaData;

        public void SetEventData(EventData eventData)
        {
            _eventaData = eventData;
        }

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
        /// エネミーのスタートポイントに出現させる
        /// </summary>
        public void TriggerEnemySpawn()
        {
            if (_spawnManager != null)
            {
                _spawnManager.SpawnEnemyAtStart();
            }
            else
            {
                Debug.LogError("EnemySpawnManagerの参照が見つかりません。");
            }
        }

        /// <summary>
        /// 指定したGameObjectの位置と回転に敵をスポーンさせる
        /// </summary>
        /// <param name="target">位置と回転の参照とするGameObject</param>
        public void SpawnAtObject(GameObject target)
        {
            if (_spawnManager != null)
            {
                if (target != null)
                {
                    SpawnAt(target.transform.position, target.transform.rotation);
                }
                else
                {
                    Debug.LogError("ターゲットのGameObjectがnullです。");
                }
            }
            else
            {
                Debug.LogError("EnemySpawnManagerの参照が見つかりません。");
            }
        }




        /// <summary>
        /// 指定した位置に直接敵をスポーンさせる
        /// </summary>
        /// <param name="position">スポーンさせる位置</param>
        /// <param name="rotation">スポーンさせる際の回転</param>
        public void SpawnAt(Vector3 position, Quaternion rotation)
        {
            if (_spawnManager != null)
            {
                _spawnManager.SpawnEnemyAtExactPosition(position, rotation, _eventaData);
            }
            else
            {
                Debug.LogError("EnemySpawnManagerの参照が見つかりません。");
            }
        }

        /// <summary>
        /// 指定した位置の近くに敵をスポーンさせる
        /// </summary>
        public void SpawnNear(Vector3 position)
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

        public void EnableBrain()
        {
            _spawnManager.EnabaleEnamy();
        }

        public void DisableBrain()
        {
            _spawnManager.DisabaleEnamy();
        }

        public void Appear()
        {
            StartCoroutine(AppearCor());
        }

        private IEnumerator AppearCor()
        {

            yield return null;
            EnableBrain();
        }

        public void Disappear()
        {
            StartCoroutine(DisappearCor());
        }

        private IEnumerator DisappearCor()
        {
            DisableBrain();

            yield return null;
        }

    }
}