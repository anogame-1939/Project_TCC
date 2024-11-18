// Assets/AnoGame/Scripts/Application/Enemy/EnemyAwareness.cs
using UnityEngine;
using UnityEngine.AI;

namespace AnoGame.Application.Enemy
{
    public class EnemyAwareness : MonoBehaviour
    {
        [SerializeField] private float spawnDistance = 5f;
        [SerializeField] private Transform player;
        private NavMeshAgent agent;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        public void SpawnNearPlayer()
        {
            Vector3 randomDirection = Random.insideUnitSphere * spawnDistance;
            randomDirection.y = 0;
            Vector3 spawnPosition = player.position + randomDirection;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, spawnDistance, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }
    }
}
