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
using AnoGame.Application.Enmemy.Control;

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
        [SerializeField] private float moveDelay = 3.0f;  // 出現前の待機時間（移動猶予）
        [SerializeField] private float destroyDelay = 5.0f;

        [SerializeField]
        private PartialFadeSettings fadeOutSettings;
        [SerializeField]
        private PartialFadeSettings fadeInSettings;
        [SerializeField] private PartialFadeSettings destroyFadeSettings;

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
                Debug.Log("既存の敵を破棄");
                var tmpEnemyInstance = _currentEnemyInstance;
                StartCoroutine(DestroyCor(tmpEnemyInstance));
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

            // _currentEnemyInstance.SetActive(false); // 初期状態では非アクティブにする
        }

        public void SetupToStoryMode()
        {
            _currentEnemyInstance.GetComponent<EnemyAIController>().SetStoryMode(true);
            // _currentEnemyInstance.GetComponent<EnemyAIController>().enabled = false;
            // _currentEnemyInstance.GetComponent<BrainBase>().enabled = false;
            // _currentEnemyInstance.GetComponent<EnemyLifespan>().enabled = false;
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Deactivate();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = true;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().EnableForceMode();
        }

        public void SetupToNormalMode()
        {
            _currentEnemyInstance.GetComponent<EnemyAIController>().SetStoryMode(false);
            // _currentEnemyInstance.GetComponent<BrainBase>().enabled = true;
            // _currentEnemyInstance.GetComponent<EnemyLifespan>().enabled = false;
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Activate();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().DisableForceMode();
        }


        // NOTE:セットアップというよりは初期化かも
        // ここではAI、当たり判定を無効化して、後のアクティベートで有効化しているので
        public void SetupToRamdomMode()
        {
            _currentEnemyInstance.GetComponent<EnemyAIController>().SetStoryMode(false);
            _currentEnemyInstance.GetComponent<EnemyAIController>().enabled = false;
            // _currentEnemyInstance.GetComponent<BrainBase>().enabled = true;
            // _currentEnemyInstance.GetComponent<EnemyLifespan>().enabled = false;
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Deactivate();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().DisableForceMode();
        }

        public void DestroyCurrentEnemyInstance()
        {
            StartCoroutine(DestroyCor(_currentEnemyInstance));
        }


        private IEnumerator DestroyCor(GameObject enemyObject)
        {
            if (enemyObject == null) yield break;

            // ① これ以上当たり判定や AI が動かないように停止
            var hit  = enemyObject.GetComponent<EnemyHitDetector>();
            if (hit) hit.enabled = false;

            var ai   = enemyObject.GetComponent<EnemyAIController>();
            if (ai)
            {
                ai.StopChasing();
                ai.enabled = false;
            }

            // ② Lifespan による部分フェード開始
            var life = enemyObject.GetComponent<EnemyLifespan>();
            if (life == null)
            {
                Destroy(enemyObject);                     // 保険
                yield break;
            }

            life.enabled = true;                         // 無効になっている場合に備えて
            life.FadeToPartialState(destroyFadeSettings);// ←ここで溶け始める

            // ③ 部分フェードが終わるまで待機
            yield return new WaitForSeconds(destroyFadeSettings.duration);

            // ④ 残りを完全に溶かす
            life.CompletePartialFadeOut(0.5f);           // 0.5 秒で完全溶解
            yield return new WaitForSeconds(10f);       // 色残り防止に少し余裕を取る

            Destroy(enemyObject);                        // ⑤ 実体を破棄
        }

        private IEnumerator DuplicateAndFade(GameObject enemyObject)
        {
            // 本体を複製（レンダラーとパーティクルだけで十分ならそれら以外の
            // Component を Remove して負荷を下げても OK）
            var corpse = Instantiate(enemyObject, enemyObject.transform.position, enemyObject.transform.rotation);

            // 本体はすぐ非表示にする
            enemyObject.SetActive(false);

            // コピーモデルにフェード処理を適用
            var life = corpse.AddComponent<EnemyLifespan>();
            life.FadeToPartialState(destroyFadeSettings);
            yield return new WaitForSeconds(destroyFadeSettings.duration);
            life.CompletePartialFadeOut(0.5f);
            yield return new WaitForSeconds(0.7f);

            Destroy(corpse);
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
            yield return null;
            Debug.Log("SpawnEnemyCoroutine-SpawnEnemyCoroutine");
            // 出現前エフェクト・効果音の再生
            // PlaySpawnedSound();
            // ※ エフェクトとしてパーティクル等を再生する処理を追加可能
            // 例: Instantiate(spawnEffectPrefab, position, rotation);

            // プレイヤーに回避の猶予を与えるため、一定時間待機（例: moveDelay秒）
            // yield return new WaitForSeconds(moveDelay);

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
            var con = _currentEnemyInstance.GetComponent<EnemyAIController>();
            con.SetChasing(true);

            Debug.Log("プレイヤーの近くに敵を出現させます。");
            yield return con.SpawnNearPlayer(playerPosition);

            // 出現前エフェクト・効果音の再生
            PlaySpawnedSound();

            Debug.Log("出現前エフェクト・効果音を再生しました。");
            // 出現前のエフェクトの待機（moveDelay秒）
            yield return new WaitForSeconds(moveDelay);

            // 敵をアクティブ化
            _currentEnemyInstance.SetActive(true);
            // _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            // プレイヤー付近での出現処理を行う
            EnabaleEnemy();
            SetEventData(null);
            // _currentEnemyController.SpawnNearPlayer(playerPosition);
            Debug.Log("プレイヤー付近に敵を出現させました。");
        }



        // NOTE: ↓↓↓ランダムスポーン関係↓↓↓
        // プレイヤーの近くに配置
        // 出現エフェクトの再生
        // 敵をアクティブ化
        // ↑この順番で処理させる

        public IEnumerator SetPositionNearPlayer(Vector3 playerPosition)
        {
            var con = _currentEnemyInstance.GetComponent<EnemyAIController>();
            con.SetChasing(true);

            Debug.Log("プレイヤーの近くに敵を出現させます。");
            yield return con.SpawnNearPlayer(playerPosition);
        }

        public void PlaySpawnedSound()
        {
            GetComponent<AudioSource>().Play();
        }

        public IEnumerator PlayrSpawnedEffect()
        {
            // 出現前エフェクト・効果音の再生
            PlaySpawnedSound();
            
            Debug.Log("出現前エフェクト・効果音を再生しました。");
            _currentEnemyInstance.gameObject.SetActive(true);
            yield return _currentEnemyInstance.GetComponent<EnemyLifespan>().PlayPartialFade(fadeOutSettings, EnemyLifespan.FadeMode.In);

            // 出現前のエフェクトの待機（moveDelay秒）
            yield return new WaitForSeconds(moveDelay);
        }

        public IEnumerator ActivateEnamy()
        {
            _currentEnemyInstance.GetComponent<EnemyAIController>().enabled = true;
            // _currentEnemyInstance.GetComponent<BrainBase>().enabled = true;
            _currentEnemyInstance.GetComponent<EnemyLifespan>().enabled = true;
            _currentEnemyInstance.GetComponent<EnemyLifespan>().Activate();
            
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Activate();

            

            yield return null;

            // _currentEnemyInstance.SetActive(true);
        }

        // NOTE: ↑↑↑ランダムスポーン関係↑↑↑

        public void SetEventData(EventData eventData)
        {
            var enemyEventController = _currentEnemyInstance.GetComponent<EnemyEventController>();
            enemyEventController.Initialize(eventData);
        }

        public void EnabaleEnemy()
        {
            // TODO:このやり方だと管理がだるいので、EnemyAIControllerの有効化処理で一元管理する
            _currentEnemyController.EnableBrain();
            _currentEnemyInstance.GetComponent<EnemyAIController>().enabled = true;

            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
        }

        public void DisabaleEnamy()
        {
            // _currentEnemyController.DisableBrain();
            _currentEnemyInstance.GetComponent<EnemyAIController>().enabled = false;
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
            return _currentEnemyInstance.GetComponent<EnemyAIController>().IsChasing;
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
