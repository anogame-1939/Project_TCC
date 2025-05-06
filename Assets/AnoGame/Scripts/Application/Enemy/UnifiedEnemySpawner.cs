using UnityEngine;
using System.Collections;
using AnoGame.Data;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

namespace AnoGame.Application.Enemy
{
    /// <summary>
    /// 敵生成のモードを管理する列挙型
    /// </summary>
    public enum SpawnerMode
    {
        Story,   // ストーリー用：追跡や自動消滅などを抑制
        Random   // ランダム用：プレイヤー追跡や時間経過による消滅など、通常の挙動を実施
    }

    /// <summary>
    /// ノーマル（ストーリー用）とランダム用の処理を統合したシンプルな敵生成クラス
    /// </summary>
    public class UnifiedEnemySpawner : MonoBehaviour
    {
        [Header("共通設定")]
        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        private SpawnerMode spawnMode = SpawnerMode.Story;



        [SerializeField]
        private EventData eventData;

        [Header("スポーンの間隔")]
        [SerializeField, Min(1f)]
        private float minSpawnTime = 15f;

        [SerializeField, Min(10f)]
        private float maxSpawnTime = 30f;

        // 追跡が続く最短／最長秒数（Inspector で調整）
        [Header("チェイス時間")]
        [SerializeField, Min(3f)]  private float minChaseTime = 5f;
        [SerializeField, Min(5f)]  private float maxChaseTime = 20f;

        // 非同期ループを止めるための CTS
        private CancellationTokenSource _spawnLoopCts;

        // 共通で利用する EnemySpawnManager（シングルトン）
        private EnemySpawnManager spawnManager;

        // 現在実行中の生成処理のコルーチン
        private IEnumerator spawnCoroutine;

        private void Awake()
        {
            spawnManager = EnemySpawnManager.Instance;
            if (spawnManager == null)
            {
                Debug.LogError("EnemySpawnManagerが見つかりません。");
            }

            // enemyPrefabを設定（各モード共通のプレハブ）
            if (enemyPrefab != null)
            {
                spawnManager.SetEnemyPrefab(enemyPrefab);
                spawnManager.InitializeEnemy();
                TriggerEnemySpawn();
            }
        }

        public void SetStoryMode()
        {
            spawnMode = SpawnerMode.Story;
            spawnManager.SetupToStoryMode();
        }

        public void SetNormalMode()
        {
            spawnMode = SpawnerMode.Story;
            spawnManager.SetupToNormalMode();
        }

        public void SetRandomMode()
        {
            spawnMode = SpawnerMode.Random;
            spawnManager.SetupToRamdomMode();
        }

        /// <summary>
        /// 外部から呼び出す敵生成開始メソッド
        /// </summary>
        public void TriggerEnemySpawn()
        {
            // 旧 Coroutine を停止
            _spawnLoopCts?.Cancel();

            if (spawnMode == SpawnerMode.Story)
            {
                spawnManager.SetupToStoryMode();
                // ストーリーは従来どおりコルーチンまたは必要に応じて UniTask 化
                StartCoroutine(StorySpawnCoroutine());
            }
            else if (spawnMode == SpawnerMode.Random)
            {
                spawnManager.SetupToRamdomMode();

                _spawnLoopCts = new CancellationTokenSource();
                RandomSpawnLoopAsync(_spawnLoopCts.Token).Forget();      // ← ★ UniTask を走らせる
            }
        }


        /// <summary>
        /// 外部から呼び出す敵生成開始メソッド
        /// </summary>
        public void TriggerEnemySpawnToStory()
        {
            spawnMode = SpawnerMode.Story;
            
            TriggerEnemySpawn();
        }

        /// <summary>
        /// ストーリーモード用の生成コルーチン
        /// 敵はスポーンするが、追跡や自動消滅機能は開始しない
        /// </summary>
        private IEnumerator StorySpawnCoroutine()
        {
            // 必要に応じ、初期化や状態リセットをここで実施可能

            yield return null;

            // isPermanentや eventData を渡して開始位置に敵を生成
            spawnManager.SetEventData(eventData);

            spawnManager.SpawnEnemyAtStart();
            yield return null;

            // Storyモードでは、敵の追跡や自動消滅処理は実施しないので、追加の処理は不要
        }

        /// <summary>
        /// Gameplay 中のみランダムに敵をスポーンし、
        /// さらにランダム時間だけチェイスさせたら Deactivate する非同期ループ
        /// </summary>
        private async UniTaskVoid RandomSpawnLoopAsync(CancellationToken token)
        {
            void OnStateChanged(GameState s)
            {
                if (s != GameState.Gameplay) _spawnLoopCts?.Cancel();
            }
            GameStateManager.Instance.OnStateChanged += OnStateChanged;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    Debug.Log($"RandomSpawnLoopAsync-GameStateManager.Instance:{GameStateManager.Instance.CurrentState}");
                    // ゲームプレイ外なら 1 フレーム待機
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    if (GameStateManager.Instance.CurrentState != GameState.Gameplay) continue;

                    float waitTime = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
                    // HACK:
                    waitTime = 3.0f;
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                    GameObject player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
                    if (player == null) continue;

                    // 1. 位置決め
                    await spawnManager.SetPositionNearPlayerAsync(player.transform.position, token);

                    // 2. 出現エフェクト
                    await spawnManager.PlaySpawnedEffectAsync(token);

                    // 3. 本体アクティベート
                    spawnManager.ActivateEnemy();
                    spawnManager.EnableChaising();

                    // 4. ランダム追跡
                    float chaseTime = UnityEngine.Random.Range(minChaseTime, maxChaseTime);
                    // HACK:
                    chaseTime = 3.0f;
                    await UniTask.Delay(TimeSpan.FromSeconds(chaseTime), cancellationToken: token);

                    var playTask = spawnManager.PlayDespawnedEffectAsync(token);

                    // １秒後に DeactivateEemy() を実行する fire-and-forget タスク
                    UniTask.Void(async () =>
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                        spawnManager.DeactivateEemy();
                    });

                    // PlayDespawnedEffectAsync の完了を待ってから DisableChashing()
                    await playTask;
                    spawnManager.DisableChashing();
                    spawnManager.DeactivateEemy();
                }
            }
            catch (OperationCanceledException)
            {
                // TODO:キャンセル処理
                Debug.Log("RandomSpawnLoopAsync-キャンセル");
                Debug.Log($"RandomSpawnLoopAsync-GameStateManager.Instance:{GameStateManager.Instance.CurrentState}");

                spawnManager.DestroyCurrentEnemyInstance();

                Debug.Log("RandomSpawnLoopAsync-おわた");



            }
            finally
            {
                GameStateManager.Instance.OnStateChanged -= OnStateChanged;
            }
        }

        /// <summary>
        /// 雑だけど怪異を部分的にフェードアウトさせるメソッド
        /// </summary>
        /// <param name="settings"></param>
        public void ApFadeToPartialStatepear(PartialFadeSettings settings)
        {
            if (settings != null)
            {
                // HACK:雑だけどここでEnemyHitDetectorを無効化しておく
                var enemyHitDetector =  spawnManager.CurrentEnemyInstance.GetComponent<EnemyHitDetector>();
                enemyHitDetector.Deactivate();

                var enemyLifespan =  spawnManager.CurrentEnemyInstance.GetComponent<EnemyLifespan>();
                // enemyLifespan.enabled = true;
                enemyLifespan.FadeToPartialState(settings);
                
            }
        }

        /// <summary>
        /// 部分フェードアウト状態になっている敵を、完全にフェードアウトさせる（消失させる）メソッド
        /// </summary>
        /// <param name="duration">完全フェードアウトにかかる時間</param>
        public void ApCompletePartialFadeOut(float duration)
        {
            var enemyLifespan = spawnManager.CurrentEnemyInstance.GetComponent<EnemyLifespan>();
            if(enemyLifespan != null)
            {
                enemyLifespan.CompletePartialFadeOut(duration);
            }
            else
            {
                Debug.LogError("CurrentEnemyInstance に EnemyLifespan コンポーネントが見つかりません。");
            }
        }
    }
}
