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
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine.AI;






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
        private PartialFadeSettings fadeInSettings;
        [SerializeField]
        private PartialFadeSettings fadeOutSettings;
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

            if (_currentEnemyInstance != null)
            {
                Debug.Log($"_currentEnemyInstance:{_currentEnemyInstance}");
                var oldInstance = _currentEnemyInstance;

                // ② 非同期でフェードアウト→破棄
                UniTask.Void(async () =>
                {
                    // ヒット判定を先にオフにしたいならここで
                    // oldInstance.GetComponent<EnemyHitDetector>().Deactivate();

                    // 古いインスタンスのフェードアウトを直接呼び出し
                    await oldInstance
                        .GetComponent<EnemyLifespan>()
                        .PlayFadeOutAsync(fadeOutSettings);

                    // フェードし終えたら破棄
                    Destroy(oldInstance);
                });
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
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Deactivate();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = true;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().EnableForceMode();
        }

        public void SetupToNormalMode()
        {
            _currentEnemyInstance.GetComponent<EnemyAIController>().SetStoryMode(false);
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Activate();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().DisableForceMode();
        }


        // NOTE:セットアップというよりは初期化かも
        // ここではAI、当たり判定を無効化して、後のアクティベートで有効化しているので
        // NOTE:なんで無効化してるのかわからないのでアクティベートするように変更
        public void SetupToRamdomMode()
        {
            _currentEnemyInstance.GetComponent<EnemyAIController>().SetStoryMode(false);
            // _currentEnemyInstance.GetComponent<EnemyAIController>().enabled = false;
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Activate();
            _currentEnemyInstance.GetComponent<ForcedMovementController>().enabled = false;
            _currentEnemyInstance.GetComponent<ForcedMovementController>().DisableForceMode();
        }

        public void DestroyCurrentEnemyInstance()
        {
            Destroy(_currentEnemyInstance);
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
        public void SpawnEnemyAtStart()
        {
            var startPoint = GetStartPoint();
            if (startPoint == null) return;

            // 出現前効果再生後に敵を出現させるコルーチンを呼び出す
            StartCoroutine(SpawnEnemyCoroutine(startPoint.position, startPoint.rotation));
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
        /// イベントとかで急に現れる時に使う
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public async UniTask SpawnfixedPsition(Vector3 position, Quaternion rotation)
        {
            Debug.Log("こっちがいいかも-SpawnEnemyCoroutine");
            _currentEnemyInstance.SetActive(true);
            _currentEnemyInstance.GetComponent<CharacterBrain>().Warp(position, rotation);

            // NOTE:これいる？？
            // _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            await PlaySpawnedEffectAsync(new CancellationToken());

            // 敵の有効化(表示、当たり判定有効化)
            ActivateEnemy();

            if (_currentEnemyController == null)
            {
                Debug.LogError("スポーンした敵にEnemyControllerが見つかりません。");
            }

            
            Debug.Log($"敵を ({position}) の位置にスポーンしました。");
        }

        public async UniTask SpawnfixedPsition_Story(Vector3 position, Quaternion rotation)
        {
            Debug.Log("こっちがいいかも-SpawnEnemyCoroutine");
            _currentEnemyInstance.SetActive(true);
            _currentEnemyInstance.GetComponent<CharacterBrain>().Warp(position, rotation);

            // NOTE:スキル用のインターフェースにすべきだが今回はこれでいく
            var treeFeller = _currentEnemyInstance.GetComponent<TreeFeller>();
            if (treeFeller != null)
            {
                treeFeller.StopSkillLoop();
            }

            await PlaySpawnedEffectAsync(new CancellationToken(), true);

            // スキル無効化

            if (_currentEnemyController == null)
            {
                Debug.LogError("スポーンした敵にEnemyControllerが見つかりません。");
            }

            
            Debug.Log($"敵を ({position}) の位置にスポーンしました。");
        }

        /// <summary>
        /// イベントとかで予め実体化していてほしい時に使う
        /// </summary>
        private IEnumerator SpawnEnemyCoroutine(Vector3 position, Quaternion rotation)
        {
            yield return null;
            Debug.Log("非推奨じゃないかもしれない-SpawnEnemyCoroutine");
            // 出現前エフェクト・効果音の再生
            // PlaySpawnedSound();
            // ※ エフェクトとしてパーティクル等を再生する処理を追加可能
            // 例: Instantiate(spawnEffectPrefab, position, rotation);

            // プレイヤーに回避の猶予を与えるため、一定時間待機（例: moveDelay秒）
            // yield return new WaitForSeconds(moveDelay);

            // 敵をアクティブ化して位置・回転を設定
            _currentEnemyInstance.SetActive(true);

            // NOTE:これいる？？
            _currentEnemyController = _currentEnemyInstance.GetComponent<EnemyController>();

            // 敵の有効化(表示、当たり判定有効化)
            ActivateEnemy();

            if (_currentEnemyController == null)
            {
                Debug.LogError("スポーンした敵にEnemyControllerが見つかりません。");
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
        }

        public void EnableChasing()
        {
            var con = _currentEnemyInstance.GetComponent<EnemyAIController>();
            con.SetChasing(true);
        }

        public void DisableChasing()
        {
            var con = _currentEnemyInstance.GetComponent<EnemyAIController>();
            con.SetChasing(false);
        }



        // NOTE: ↓↓↓ランダムスポーン関係↓↓↓
        // プレイヤーの近くに配置
        // 出現エフェクトの再生
        // 敵をアクティブ化
        // ↑この順番で処理させる

        public async UniTask SetPositionNearPlayerAsync(Vector3 playerPos, CancellationToken token)
        {
            var con = _currentEnemyInstance.GetComponent<EnemyAIController>();
            await con.SpawnNearPlayerAsync(playerPos);   // ★IEnumerator → UniTask 化
        }

        public void PlaySpawnedSound()
        {
            GetComponent<AudioSource>().Play();
        }

        /// <summary>
        /// 出現 SE とフェードイン演出を実時間で待機しながら再生する
        /// </summary>
        public async UniTask PlaySpawnedEffectAsync(CancellationToken token, bool noSound = false)
        {
            if (!noSound) PlaySpawnedSound();                // SE 再生

            _currentEnemyInstance.GetComponent<NavMeshAgent>().enabled = false;

            // Lifespan 側も UniTask 版を用意してある前提
            await _currentEnemyInstance
                .GetComponent<EnemyLifespan>()
                .PlayFadeInAsync(fadeInSettings);

            await UniTask.Delay(TimeSpan.FromSeconds(moveDelay), cancellationToken: token);

            _currentEnemyInstance.GetComponent<NavMeshAgent>().enabled = true;
        }

        /// <summary>
        /// 消滅 SE とフェードアウト演出
        /// </summary>
        public async UniTask PlayDespawnedEffectAsync(CancellationToken token)
        {
            // 当たり判定を即座に無効化
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Deactivate();

            await _currentEnemyInstance
                .GetComponent<EnemyLifespan>()
                .PlayFadeOutAsync(fadeOutSettings);
        }

        public async UniTask PlayDespawnedEffectAsync(PartialFadeSettings originSettings, CancellationToken token)
        {
            // 当たり判定を即座に無効化
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Deactivate();

            await _currentEnemyInstance
                .GetComponent<EnemyLifespan>()
                .PlayFadeOutAsync(originSettings);
        }

        public async UniTask PlayDespawnedEffectLoopAsync(PartialFadeSettings originSettings, CancellationToken token)
        {
            await _currentEnemyInstance
                .GetComponent<EnemyLifespan>()
                .PlayFadeOutLoopAsync(originSettings);
        }

        public async UniTask PlayDespawnedEffectLoopEndAsync(float duration)
        {
            await _currentEnemyInstance
                .GetComponent<EnemyLifespan>()
                .PlayFadeOutLoopEndAsync(duration);
        }


        public void ActivateEnemy()
        {
            var life = _currentEnemyInstance.GetComponent<EnemyLifespan>();
            life.Activate();

            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Activate();

            // NOTE:スキル用のインターフェースにすべきだが今回はこれでいく
            var treeFeller = _currentEnemyInstance.GetComponent<TreeFeller>();
            if (treeFeller != null)
            {
                treeFeller.PlaySkillLoop();
            }
        }

        public void DeactivateEnemy()
        {
            // _currentEnemyInstance.GetComponent<BrainBase>().enabled = true;
            _currentEnemyInstance.GetComponent<EnemyLifespan>().Deactivate();
            _currentEnemyInstance.GetComponent<EnemyHitDetector>().Deactivate();
            // _currentEnemyInstance.SetActive(true);

            // NOTE:スキル用のインターフェースにすべきだが今回はこれでいく
            var treeFeller = _currentEnemyInstance.GetComponent<TreeFeller>();
            if (treeFeller != null)
            {
                treeFeller.StopSkillLoop();
            }
        }

        // NOTE: ↑↑↑ランダムスポーン関係↑↑↑

        public void SetEventData(EventData eventData)
        {
            var enemyEventController = _currentEnemyInstance.GetComponent<EnemyEventController>();
            enemyEventController.Initialize(eventData);
        }

        public void ScuccesEvent()
        {
            var enemyEventController = _currentEnemyInstance.GetComponent<EnemyEventController>();
            enemyEventController.HandleEscapeSuccess();
        }
        

        public void DisabaleEnamy()
        {
            // _currentEnemyController.DisableBrain();
            _currentEnemyInstance.GetComponent<EnemyAIController>().SetChasing(false);
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
