using UnityEngine;
using UnityEngine.AI;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float spawnDistance = 5f;
        private CharacterBrain _characterBrain;
        private NavMeshAgent _agent;

        void Start()
        {
            // CharacterBrainコンポーネントの取得とNullチェック
            _characterBrain = GetComponent<CharacterBrain>();
            if (_characterBrain == null)
            {
                Debug.LogError($"CharacterBrainが設定されていません。: {gameObject.name}");
                return;
            }

            // NavMeshAgentの取得
            _agent = GetComponentInChildren<NavMeshAgent>();
            if (_agent == null)
            {
                Debug.LogError($"NavMeshAgentが設定されていません。: {gameObject.name}");
                return;
            }

            // キャラクターの動きを止める
            // StopMoving();
        }

        public void EnableBrain()
        {
            GetComponent<CharacterBrain>().enabled = true;
        }

        public void DisableBrain()
        {
            GetComponent<CharacterBrain>().enabled = false;
        }

        /// <summary>
        /// プレイヤーの近くの有効な位置にスポーンする
        /// </summary>
        /// <param name="playerPosition">プレイヤーの位置</param>
        public void SpawnNearPlayer(Vector3 playerPosition)
        {
            Vector3 randomDirection = Random.insideUnitSphere * spawnDistance;
            randomDirection.y = 0;
            Vector3 spawnPosition = playerPosition + randomDirection;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, spawnDistance, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                StartMoving(); // スポーン後に動き出す
            }
            else
            {
                Debug.LogWarning("有効なスポーン位置が見つかりませんでした。");
            }
        }

        /// <summary>
        /// キャラクターの動きを開始させる
        /// </summary>
        public void StartMoving()
        {
            _characterBrain.enabled = true;
            if (_agent != null)
            {
                _agent.isStopped = false;
            }
        }

        /// <summary>
        /// キャラクターの動きを停止させる
        /// </summary>
        public void StopMoving()
        {
            _characterBrain.enabled = false;
            if (_agent != null)
            {
                _agent.isStopped = true;
            }
        }

        /// <summary>
        /// キャラクターが動いているかどうかを取得
        /// </summary>
        public bool IsMoving => _characterBrain != null && _characterBrain.enabled;
    }
}