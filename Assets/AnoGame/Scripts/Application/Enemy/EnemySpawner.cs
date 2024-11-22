using UnityEngine;

namespace AnoGame.Application.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        // EnemyControllerのインスタンスを取得するために必要
        [SerializeField] private EnemyController enemyController;

        public void TriggerEnemySpawn()
        {
            if (enemyController != null)
            {
                enemyController.SpawnNearPlayer(transform.position);
            }
            else
            {
                Debug.LogError("EnemyController reference is missing in EnemySpawner!");
            }
        }
    }
}