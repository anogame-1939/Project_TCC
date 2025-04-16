using UnityEngine;
using AnoGame.Data;
using VContainer;
using AnoGame.Application.Core;
using AnoGame.Application.Event;
using AnoGame.Domain.Event.Services;
using UnityEngine.SceneManagement;
using AnoGame.Application.Story;
using Unity.TinyCharacterController.Brain;
using System.Collections;
using AnoGame.Application.Player.Control;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Enemy
{
    public class EnemySpawnManager : SingletonMonoBehaviour<EnemySpawnManager>
    {
        [Inject] private IEventService _eventService;
        [Inject] private EventManager _eventManager;

        [Inject]
        public void Construct(
            IEventService eventService,
            EventManager eventManager
        )
        {
            _eventService = eventService;
            _eventManager = eventManager;
        }

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float moveDelay = 5.0f;  // 出現前の待機時間（移動猶予）
        [SerializeField] private float destroyDelay = 5.0f;
        private GameObject _currentEnemyInstance;
        public GameObject CurrentEnemyInstance => _currentEnemyInstance;
        private EnemyController _currentEnemyController;
        
        private const string TAG_START_POINT_ENEMY = "StartPointEnemy";
        private const string TAG_RETRY_POINT_ENEMY = "RetryPointEnemy";
        
        private Transform _currentRetryPoint;

        private void Start()
        {
            // スタートポイントから初期スポーン
            // SpawnEnemyAtStart();
        }

        // TODO:チャプター毎に生成する怪異を付け替える
        public void InitializeEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("Enemy Prefabが設定されていません。");
                return;
            }

            // 既存の敵を破棄
            if (_currentEnemyInstance != null)
            {
                StartCoroutine(DestroyCor(_currentEnemyInstance));
                _currentEnemyController = null;
            }

            // 新しい敵をスポーン（※初期状態では非アクティブにしておくなど、事前設定が必要な場合はこちらで対応）
            _currentEnemyInstance = Instantiate(enemyPrefab);
            _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            var enemyEventController = _currentEnemyInstance.GetComponent<EnemyEventController>();
            enemyEventController.Construct(_eventService, _eventManager);
            // container.InjectGameObject(_currentEnemyInstance);

            // 明示的にメインシーンに配置する
            var targetScene = StoryManager.Instance.MainScene;
            SceneManager.MoveGameObjectToScene(_currentEnemyInstance, targetScene);
        }

        public void SetupToStoryMode()
        {
            _currentEnemyInstance.GetComponent<EnemyLifespan>().enabled = false;
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().enabled = false;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = true;
        }

        public void SetupToRamdomMode()
        {
            _currentEnemyInstance.GetComponent<EnemyLifespan>().enabled = true;
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().enabled = true;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
        }

        public void DestroyCurrentEnemyInstance()
        {
            StartCoroutine(DestroyCor(_currentEnemyInstance));
        }

        private IEnumerator DestroyCor(GameObject enemyObject)
        {
            enemyObject.GetComponent<EnemyHitDetector>().enabled = false;
            enemyObject.GetComponent<EnemyLifespan>().enabled = true;
            enemyObject.GetComponent<EnemyLifespan>().TriggerFadeOutAndDestroy();

            yield return new WaitForSeconds(destroyDelay);
            Destroy(enemyObject);
        }

        public void SetEnemyPrefab(GameObject prefab)
        {
            enemyPrefab = prefab;
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

        /// <summary>
        /// スタート地点に敵を出現させる。
        /// </summary>
        public void SpawnEnemyAtStart(bool isPermanent = false)
        {
            var startPoint = GetStartPoint();
            if (startPoint == null) return;

            // 出現前効果再生後に敵を出現させるコルーチンを呼び出す
            StartCoroutine(SpawnEnemyCoroutine(startPoint.position, startPoint.rotation, isPermanent));
        }

        /// <summary>
        /// リトライ地点に敵を出現させる。
        /// </summary>
        public void SpawnEnemyAtRetryPoint()
        {
            Transform targetPoint = _currentRetryPoint ?? GetRetryPoint();
            
            if (targetPoint == null)
            {
                Debug.LogWarning("リトライポイントが見つからないため、スタートポイントを使用します。");
                SpawnEnemyAtStart();
                return;
            }

            StartCoroutine(SpawnEnemyCoroutine(targetPoint.position, targetPoint.rotation));
        }

        /// <summary>
        /// 出現前のエフェクト・効果音再生後、一定時間待機してから敵を出現させるコルーチン
        /// </summary>
        private IEnumerator SpawnEnemyCoroutine(Vector3 position, Quaternion rotation, bool isPermanent = false)
        {
            Debug.Log("SpawnEnemyCoroutine-SpawnEnemyCoroutine");
            // 出現前エフェクト・効果音の再生
            PlaySpawnedSound();
            // ※ エフェクトとしてパーティクル等を再生する処理を追加可能
            // 例: Instantiate(spawnEffectPrefab, position, rotation);

            // プレイヤーに回避の猶予を与えるため、一定時間待機（例: moveDelay秒）
            yield return new WaitForSeconds(moveDelay);

            // 敵をアクティブ化して位置・回転を設定
            _currentEnemyInstance.SetActive(true);
            _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            if (_currentEnemyController == null)
            {
                Debug.LogError("スポーンした敵にEnemyControllerが見つかりません。");
            }
            
            if (isPermanent)
            {
                _currentEnemyController.GetComponent<EnemyLifespan>().enabled = false;
            }
            else
            {
                _currentEnemyController.GetComponent<EnemyLifespan>().enabled = true;
            }
            
            _currentEnemyInstance.GetComponent<CharacterBrain>().Warp(position, rotation);
            Debug.Log($"敵を ({position}) の位置にスポーンしました。");
        }

        /// <summary>
        /// 特定の位置に敵を出現させる（エフェクト再生後に実施）。
        /// イベントデータが設定されている場合は、出現後に反映する。
        /// </summary>
        public void SpawnEnemyAtExactPosition(Vector3 position, Quaternion rotation, EventData eventData = null)
        {
            StartCoroutine(SpawnEnemyAtExactPositionCoroutine(position, rotation, eventData));
        }

        private IEnumerator SpawnEnemyAtExactPositionCoroutine(Vector3 position, Quaternion rotation, EventData eventData)
        {
            yield return SpawnEnemyCoroutine(position, rotation);
            if (eventData != null)
            {
                SetEventData(eventData);
            }
            EnabaleEnemy();
        }

        /// <summary>
        /// プレイヤー付近に敵を出現させる。
        /// 出現前効果再生後、一定時間待機してからEnemyControllerのSpawnNearPlayerを呼び出すコルーチンを実行する。
        /// </summary>
        public void SpawnEnemyNearPlayer(Vector3 playerPosition)
        {
            StartCoroutine(SpawnEnemyNearPlayerCoroutine(playerPosition));
        }

        private IEnumerator SpawnEnemyNearPlayerCoroutine(Vector3 playerPosition)
        {
            Debug.Log("SpawnEnemyCoroutine-SpawnEnemyNearPlayerCoroutine");
            // 出現前エフェクト・効果音の再生
            PlaySpawnedSound();
            // 出現前のエフェクトの待機（moveDelay秒）
            yield return new WaitForSeconds(moveDelay);

            // 敵をアクティブ化
            _currentEnemyInstance.SetActive(true);
            _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            // プレイヤー付近での出現処理を行う
            EnabaleEnemy();
            SetEventData(null);
            _currentEnemyController.SpawnNearPlayer(playerPosition);
            Debug.Log("プレイヤー付近に敵を出現させました。");
        }

        public void PlaySpawnedSound()
        {
            GetComponent<AudioSource>().Play();
        }

        // ※ 既存の SpawnEnemyAt メソッドは、SpawnEnemyCoroutine に処理を分散しています。
        private void SpawnEnemyAt(Vector3 position, Quaternion rotation, bool isPermanent = false)
        {
            _currentEnemyInstance.SetActive(true);
            _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            if (isPermanent)
            {
                _currentEnemyController.GetComponent<EnemyLifespan>().enabled = false;
            }
            else
            {
                _currentEnemyController.GetComponent<EnemyLifespan>().enabled = true;
            }
            
            if (_currentEnemyController == null)
            {
                Debug.LogError("スポーンした敵にEnemyControllerが見つかりません。");
            }

            _currentEnemyInstance.GetComponent<CharacterBrain>().Warp(position, rotation);
            Debug.Log($"敵を ({position}) の位置にスポーンしました。");
        }

        public void SetEventData(EventData eventData)
        {
            var enemyEventController = _currentEnemyInstance.GetComponent<EnemyEventController>();
            enemyEventController.Initialize(eventData);
        }

        public void EnabaleEnemy()
        {
            _currentEnemyController.EnableBrain();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
        }

        public void DisabaleEnamy()
        {
            _currentEnemyController.DisableBrain();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = true;
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

        public bool IsChasing()
        {
            return _currentEnemyInstance.GetComponent<EnemyEventController>().IsChasing;
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
