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

            spawnManager.SpawnEnemyAtStart(true);
            yield return null;

            // Storyモードでは、敵の追跡や自動消滅処理は実施しないので、追加の処理は不要
        }

        /// <summary>
        /// ランダムモード用の生成コルーチン
        /// トリガーされたら、ランダムな間隔で継続的に敵を生成し、通常の追跡・タイムアウト消滅処理を実行
        /// </summary>
        private IEnumerator RandomSpawnCoroutine()
        {
            Debug.Log("RandomSpawnCoroutine - ");
            // 終了条件がある場合はそれに合わせてループ条件を変更してください。
            while (true)
            {
                // ランダムな待機時間を設定
                float waitTime = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
                Debug.Log("RandomSpawnCoroutine - wait" + waitTime);
                // yield return new WaitForSeconds(waitTime);
                yield return new WaitForSeconds(1f);

                Debug.Log("RandomSpawnCoroutine - start" + waitTime);

                

                // プレイヤーをタグから検索し、近くに敵をスポーン
                GameObject player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);

                if (player != null)
                {
                    yield return spawnManager.SetPositionNearPlayer(player.transform.position);

                    yield return spawnManager.PlayrSpawnedEffect();
                    yield return spawnManager.ActivateEnamy();

                }
                else
                {
                    Debug.LogError("プレイヤーオブジェクトが見つかりません。");
                }

                // スポーン時の

                // ランダムモードでは、敵の追跡やタイムアウト消滅など、通常の機能を有効にする
                // 脳の有効化や移動開始処理を実施
                // spawnManager.EnabaleEnemy();
                // spawnManager.StartEnemyMovement();

                // 敵が追跡状態にある間、待機（IsChasing() が false になれば次のループへ）
                // while (spawnManager.IsChasing())
                {
                    Debug.Log("Enemy is chasing...");
                    yield return new WaitForSeconds(1f);
                }

                Debug.Log("Enemy has stopped chasing.");

                // 状態が変わった場合に移動停止などの後処理を実施
                // spawnManager.StopEnemyMovement();

                yield return new WaitForSeconds(10f);
            }
        }

    /// <summary>
    /// Gameplay 中のみランダムに敵をスポーンし、
    /// さらにランダム時間だけチェイスさせたら Deactivate する非同期ループ
    /// </summary>
    private async UniTaskVoid RandomSpawnLoopAsync(CancellationToken token)
    {
        // GameState が変わったらキャンセルするハンドラ
        void OnStateChanged(GameState s)
        {
            if (s != GameState.Gameplay) _spawnLoopCts?.Cancel();
        }
        GameStateManager.Instance.OnStateChanged += OnStateChanged;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // ── ❶ Gameplay でなければ 1 フレーム待機 ─────────────
                    if (GameStateManager.Instance.CurrentState != GameState.Gameplay)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                        continue;
                    }

                    // ── ❷ 次回スポーンまでの待機 ─────────────────────
                    float waitTime = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
                    // HACK:
                    waitTime = 3.0f;
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                    // ── ❸ プレイヤーを取得できなければスキップ ───────────
                    GameObject player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
                    if (player == null) continue;

                    // ── ❹ 敵をスポーン ─────────────────────────────
                    await spawnManager.SetPositionNearPlayer(player.transform.position);
                    await spawnManager.PlayrSpawnedEffect();
                    await spawnManager.ActivateEnamy();
                    spawnManager.EnableChaising();

                    // ── ❺ ランダムチェイス時間が経過するまで待機 ──────────
                    float chaseTime = UnityEngine.Random.Range(minChaseTime, maxChaseTime);
                    // HACK:
                    chaseTime = 3.0f;
                    Debug.Log("逃げてる");
                    await UniTask.Delay(TimeSpan.FromSeconds(chaseTime), cancellationToken: token);
                    Debug.Log("逃げ切った");


                    // ── ❻ 追跡終了処理（フェードアウトなど） ─────────────

                    spawnManager.DisableChashing();

                    await spawnManager.PlayrDeSpawnedEffect();
                    await spawnManager.DeactivateEnamy();
                }
            }
            catch (OperationCanceledException oce)
            {
                // TODO:キャンセル処理を書く
                Debug.Log($"キャンセルされた...{oce.Message}, {oce.StackTrace}");
            
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
