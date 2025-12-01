using UnityEngine;
using AnoGame.Application.Core;

namespace AnoGame.Application.Enemy
{
    public class EnemySpawnManager : SingletonMonoBehaviour<EnemySpawnManager>
    {
        [SerializeField]
        private GameObject _currentEnemyInstance;
        public GameObject CurrentEnemyInstance => _currentEnemyInstance;

        public void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Debug.Log("prefab " + prefab);
            if (prefab == null) return;

            // 既存の敵がいれば削除するなどの処理が必要ならここに追加
            if (_currentEnemyInstance != null)
            {
                Destroy(_currentEnemyInstance);
            }

            _currentEnemyInstance = Instantiate(prefab, position, rotation);
        }
    }
}
