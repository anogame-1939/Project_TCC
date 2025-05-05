using UnityEngine;
using System.Collections;
using AnoGame.Data;

namespace AnoGame.Application.Enemy
{
    public class EnemyRandomSpawner : MonoBehaviour
    {
        [SerializeField] 
        [Min(1f)]  // 最小値を0に制限
        private float minSpawnTime = 15f;   // 最小生成間隔（秒）
        
        [SerializeField]
        [Min(10f)]  // 最小値を0に制限
        private float maxSpawnTime = 30f;   // 最大生成間隔（秒）

        private EnemySpawnManager _spawnManager;
        private IEnumerator SpawnCoroutine;

        private void Awake()
        {
            _spawnManager = EnemySpawnManager.Instance;
            if (_spawnManager == null)
            {
                Debug.LogError("EnemySpawnManagerが見つかりません。");
            }
            GameManager2.Instance.GameOver += StopSpawner;
            
        }

        public void StartSpawner()
        {
            Debug.Log("StartSpawner");
            SpawnCoroutine = StartSpawnerCor();
            StartCoroutine(SpawnCoroutine);
        }

        public void StopSpawner()
        {
            Debug.Log("GameOver -> StopSpawner");
            StopCoroutine(SpawnCoroutine);
        }

        private IEnumerator StartSpawnerCor()
        {
            while (true)
            {
                Debug.Log("来る…？");
                // minSpawnTimeとmaxSpawnTimeの間でランダムな時間を生成
                float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
                yield return new WaitForSeconds(waitTime);

                Debug.Log($"出たぁ！！:{waitTime}");
                SpawnNearPlayer();
                StartEnemyMovement();
                

                Debug.Log("逃走中…");

                yield return WaitForEnemyDeath();

                Debug.Log("怪異消滅");
                break;
            }
        }

        private void SpawnNearPlayer()
        {
            var player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
            SpawnNear(player.transform.position);
        }

        private IEnumerator WaitForEnemyDeath()
        {
            yield return null;
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
                _spawnManager.SpawnEnemyAtExactPosition(position, rotation);
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
                // _spawnManager.SpawnEnemyNearPlayer(position);
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
            _spawnManager.EnabaleEnemy();
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

#if UNITY_EDITOR
        // エディタ上でmaxSpawnTimeがminSpawnTimeより小さくならないようにする
        private void OnValidate()
        {
            if (maxSpawnTime < minSpawnTime)
            {
                maxSpawnTime = minSpawnTime;
            }
        }
#endif

    }
}