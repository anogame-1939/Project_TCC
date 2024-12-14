using UnityEngine;
using AnoGame.Infrastructure;
using AnoGame.Application.Enemy;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Story
{
    public class EnemySpawnManager : SingletonMonoBehaviour<EnemySpawnManager>
    {   
    [SerializeField] private GameObject enemyPrefab;
    private GameObject _currentEnemyInstance;
    private EnemyController _currentEnemyController;  // EnemyControllerへの参照を保持
    
    private const string TAG_START_POINT_ENEMY = "StartPointEnemy";
    private const string TAG_RETRY_POINT_ENEMY = "RetryPointEnemy";
    
    private Transform _currentRetryPoint;

        private void Start()
        {
            InitializeEnemy();
        }

        private void InitializeEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("Enemy Prefabが設定されていません。");
                return;
            }

            // スタートポイントから初期スポーン
            SpawnEnemyAtStart();
        }

        private Transform GetStartPoint()
        {
            var startPoint = GameObject.FindWithTag(TAG_START_POINT_ENEMY);
            if (startPoint == null)
            {
                Debug.LogError("StartPointEnemyタグのついたオブジェクトが見つかりません。");
            }
            return startPoint?.transform;
        }

        private Transform GetRetryPoint()
        {
            var retryPoint = GameObject.FindWithTag(TAG_RETRY_POINT_ENEMY);
            return retryPoint?.transform;
        }

        public void OnChapterLoaded(bool useRetryPoint)
        {
            if (useRetryPoint)
            {
                Debug.Log("リトライポイントから敵を配置します。");
                SpawnEnemyAtRetryPoint();
            }
            else
            {
                Debug.Log("スタートポイントから敵を配置します。");
                SpawnEnemyAtStart();
            }
        }

        public void SpawnEnemyAtStart()
        {
            var startPoint = GetStartPoint();
            if (startPoint == null) return;

            SpawnEnemyAt(startPoint.position, startPoint.rotation);
            // EnabaleEnamy();
        }

        public void SpawnEnemyAtRetryPoint()
        {
            Transform targetPoint = _currentRetryPoint ?? GetRetryPoint();
            
            if (targetPoint == null)
            {
                Debug.LogWarning("リトライポイントが見つからないため、スタートポイントを使用します。");
                SpawnEnemyAtStart();
                return;
            }

            SpawnEnemyAt(targetPoint.position, targetPoint.rotation);
            // EnabaleEnamy();
        }

        private void SpawnEnemyAt(Vector3 position, Quaternion rotation)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("Enemy Prefabが設定されていません。");
                return;
            }

            // 既存の敵を破棄
            if (_currentEnemyInstance != null)
            {
                Destroy(_currentEnemyInstance);
                _currentEnemyController = null;
            }

            // 新しい敵をスポーン
            _currentEnemyInstance = Instantiate(enemyPrefab, position, rotation);
            // EnemyControllerの参照を保持
            _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();
            
            if (_currentEnemyController == null)
            {
                Debug.LogError("スポーンした敵にEnemyControllerが見つかりません。");
            }

            Debug.Log($"敵を ({position}) の位置にスポーンしました。");
        }

        public void EnabaleEnamy()
        {
            _currentEnemyController.EnableBrain();
        }

        public void DisabaleEnamy()
        {
            _currentEnemyController.DisableBrain();
        }

        // 敵の状態を制御するためのメソッド群
        public void StartEnemyMovement()
        {
            if (_currentEnemyController != null)
            {
                _currentEnemyController.StartMoving();
            }
        }

        public void StopEnemyMovement()
        {
            if (_currentEnemyController != null)
            {
                _currentEnemyController.StopMoving();
            }
        }

        public void SpawnEnemyAtExactPosition(Vector3 position, Quaternion rotation)
        {
            SpawnEnemyAt(position, rotation);
            EnabaleEnamy();  // 既存の仕様に合わせてスポーン後に有効化
        }

        public void SpawnEnemyNearPlayer(Vector3 playerPosition)
        {
            if (_currentEnemyController != null)
            {
                _currentEnemyController.SpawnNearPlayer(playerPosition);
            }
        }

        public void SetRetryPoint(Transform point)
        {
            if (point != null)
            {
                _currentRetryPoint = point;
                Debug.Log($"敵のリトライポイントを {point.name} に設定しました。");
            }
        }

        public void SetCurrentPositionAsRetryPoint()
        {
            if (_currentEnemyInstance == null)
            {
                Debug.LogError("敵インスタンスが存在しません。");
                return;
            }

            GameObject retryPointObj = new GameObject("DynamicEnemyRetryPoint");
            retryPointObj.tag = TAG_RETRY_POINT_ENEMY;
            retryPointObj.transform.position = _currentEnemyInstance.transform.position;
            retryPointObj.transform.rotation = _currentEnemyInstance.transform.rotation;

            _currentRetryPoint = retryPointObj.transform;
            Debug.Log("敵の現在位置をリトライポイントとして設定しました。");
        }

#if UNITY_EDITOR
        [ContextMenu("Reset To Start Point")]
        public void ExecuteSpawnAtStart()
        {
            SpawnEnemyAtStart();
        }

        [ContextMenu("Use Retry Point")]
        public void ExecuteSpawnAtRetryPoint()
        {
            SpawnEnemyAtRetryPoint();
        }

        [ContextMenu("Set Current As Retry Point")]
        public void ExecuteSetCurrentAsRetryPoint()
        {
            SetCurrentPositionAsRetryPoint();
        }

        [CustomEditor(typeof(EnemySpawnManager))]
        public class EnemySpawnManagerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                EnemySpawnManager manager = (EnemySpawnManager)target;

                if (GUILayout.Button("Reset To Start Point"))
                {
                    manager.SpawnEnemyAtStart();
                }

                if (GUILayout.Button("Use Retry Point"))
                {
                    manager.SpawnEnemyAtRetryPoint();
                }

                if (GUILayout.Button("Set Current As Retry Point"))
                {
                    manager.SetCurrentPositionAsRetryPoint();
                }
            }
        }
#endif
    }
}