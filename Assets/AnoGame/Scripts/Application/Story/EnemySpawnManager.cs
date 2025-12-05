using UnityEngine;
using AnoGame.Application.Core;
using AnoGame.Application.Enemy.AI;
using UnityEngine.Splines;
using VContainer;
using VContainer.Unity;

namespace AnoGame.Application.Enemy
{
    public class EnemySpawnManager : SingletonMonoBehaviour<EnemySpawnManager>
    {
        [SerializeField]
        private GameObject _currentEnemyInstance;
        public GameObject CurrentEnemyInstance => _currentEnemyInstance;

        private IPatrolRouteRegistry _patrolRouteRegistry;
        private IObjectResolver _objectResolver;

        [Inject]
        public void Construct(IPatrolRouteRegistry registry, IObjectResolver objectResolver)
        {
            _patrolRouteRegistry = registry;
            _objectResolver = objectResolver;
        }

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
            _objectResolver.InjectGameObject(_currentEnemyInstance);

            // パトロールルートの注入
            if (_patrolRouteRegistry != null)
            {
                var spline = _patrolRouteRegistry.GetPatrolRoute();
                if (spline != null)
                {
                    var returnToAnchor = _currentEnemyInstance.GetComponentInChildren<AnoGame.Application.Enemy.AI.ReturnToAnchorIntentProvider>();
                    if (returnToAnchor != null) returnToAnchor.Initialize(spline);

                    var patrolSpline = _currentEnemyInstance.GetComponentInChildren<AnoGame.Application.Enemy.AI.PatrolSplineIntentProvider>();
                    if (patrolSpline != null) patrolSpline.Initialize(spline);
                }
            }
        }
    }
}
