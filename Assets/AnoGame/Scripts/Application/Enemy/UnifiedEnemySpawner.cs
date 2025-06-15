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

        private CancellationTokenSource _despawnTimerCts;

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
            }
        }

        public void SetStoryMode()
        {
            Debug.LogError("廃止-いや、まて。。");
            spawnManager.SetupToStoryMode();
        }

        public void SetNormalMode()
        {
            Debug.LogError("廃止");
            spawnManager.SetupToNormalMode();
        }

        public void SetRandomMode()
        {
            Debug.LogError("廃止");
            spawnManager.SetupToRamdomMode();
        }

        public void StorySpawn()
        {
            spawnManager.SetupToStoryMode();

            spawnManager.SpawnEnemyAtStart();
        }

        public void SetEventData(EventData eventData)
        {
            spawnManager.SetEventData(eventData);
        }

        public async void SpawnfixedPsition(Transform position)
        {
            await spawnManager.SpawnfixedPsition(position.position, position.localRotation);
            spawnManager.EnableChasing();
        }

        public async void SpawnfixedPsition_Story(Transform position)
        {
            await spawnManager.SpawnfixedPsition_Story(position.position, position.localRotation);
        }

        /// <summary>
        /// 引数の秒数経過後、敵をフェードアウト＆デスポーンさせるタイマーを開始する。
        /// すでにタイマーが走っている場合は上書きする。
        /// </summary>
        /// <param name="seconds">フェードアウト開始までの待機秒数</param>
        public void StartDespawnTimer(float seconds)
        {
            // 前回のタイマーがあれば停止
            _despawnTimerCts?.Cancel();
            _despawnTimerCts?.Dispose();

            _despawnTimerCts = new CancellationTokenSource();
            // fire‑and‑forget で非同期処理へ
            HandleDespawnTimerAsync(seconds, _despawnTimerCts.Token).Forget();
        }

        /// <remarks>
        /// ・敵が存在しない、または SpawnManager が null の場合は何もしない
        /// ・フェードアウト演出中でも CTS がキャンセルされたら即終了
        /// </remarks>
        private async UniTaskVoid HandleDespawnTimerAsync(float seconds, CancellationToken token)
        {
            void OnStateChanged(GameState s)
            {
                // TODO:ゲームオーバーではなく会話シーンに突入したときにする。ゲームオーバー時の敵削除は別でやる
                if (s == GameState.GameOver) _despawnTimerCts?.Cancel();
            }
            GameStateManager.Instance.OnStateChanged += OnStateChanged;
            try
            {
                // 指定秒数待機
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);

                // ここでチェイス停止
                spawnManager.DisableChasing();
                Debug.Log("逃げ切り成功");

                // 敵がまだ生きているか確認
                if (spawnManager?.CurrentEnemyInstance == null) return;

                // フェードアウト演出開始
                var playTask = spawnManager.PlayDespawnedEffectAsync(token);

                // 1 秒後に DeactivateEemy() を実行
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                    spawnManager.DeactivateEnemy();
                });

                // エフェクト完了を待つ
                await playTask;

                // 念のため Deactivate
                
                spawnManager.DeactivateEnemy();

                // EventDataを成功とする
                spawnManager.ScuccesEvent();
                
            }
            catch (OperationCanceledException)
            {
                Debug.Log("逃げ切り失敗");
                // タイマーが途中でキャンセルされた場合は何もしない
            }
        }





        /// <summary>
        /// 外部から呼び出す敵生成開始メソッド
        /// </summary>
        public void TriggerEnemySpawnToStory()
        {
            Debug.LogError("廃止");
        }

        private CancellationTokenSource _outerCts;   // 監視トラック
        private CancellationTokenSource _innerCts;   // いま実行中の 1 回分

        // ───────────────────────────
        //  ランダムスポーン監視トラック
        // ───────────────────────────
        public void RandomSpawnLoop()
        {
            if (_outerCts != null) return;

            Debug.Log("[Spawner] RandomSpawnLoop START");
            spawnManager.SetupToRamdomMode();

            _outerCts = new CancellationTokenSource();
            MonitorGameplayStateAsync(_outerCts.Token).Forget();
        }

        public async void CancelRandomSpawnLoop()
        {
            Debug.Log("[Spawner] RandomSpawnLoop CANCEL 要求");

            _outerCts?.Cancel();
            _outerCts = null;

            _innerCts?.Cancel();
            _innerCts = null;

            spawnManager.DisableChasing();
            await spawnManager.PlayDespawnedEffectAsync(default);
            spawnManager.DeactivateEnemy();

            Debug.Log("[Spawner] RandomSpawnLoop 完全停止");
        }

        private async UniTaskVoid MonitorGameplayStateAsync(CancellationToken outerToken)
        {
            void OnStateChanged(GameState s)
            {
                Debug.Log($"[Spawner] GameState 変化 → {s}");

                if (s != GameState.Gameplay ||
                    GameStateManager.Instance.CurrentSubState == GameSubState.Safety)
                {
                    Debug.Log("[Spawner] Gameplay 外 → _innerCts.Cancel()");
                    _innerCts?.Cancel();
                }
                else if (_innerCts == null || _innerCts.IsCancellationRequested)
                {
                    Debug.Log("[Spawner] Gameplay 突入 → LaunchSpawnCycle()");
                    LaunchSpawnCycle();
                }
            }

            GameStateManager.Instance.OnStateChanged += OnStateChanged;

            // 初回チェック
            OnStateChanged(GameStateManager.Instance.CurrentState);

            Debug.Log("[Spawner] MonitorGameplayStateAsync 待機開始");
            await WaitUntilCanceled(outerToken);
            Debug.Log("[Spawner] MonitorGameplayStateAsync 終了");

            GameStateManager.Instance.OnStateChanged -= OnStateChanged;
            _innerCts?.Cancel();
            _innerCts = null;
        }

        private async UniTask WaitUntilCanceled(CancellationToken token)
        {
            try
            {
                await UniTask.WaitUntilCanceled(token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[Spawner] outerToken Cancelled");
            }
        }

        // ───────────────────────────
        //  1 サイクル
        // ───────────────────────────
        private void LaunchSpawnCycle()
        {
            _innerCts = new CancellationTokenSource();
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_outerCts.Token, _innerCts.Token);

            Debug.Log("[Spawner] --- SpawnCycle START ---");
            SpawnCycleAsync(linked.Token).Forget();
        }

        private void LaunchSpawnLoop()
        {
            _innerCts = new CancellationTokenSource();
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_outerCts.Token, _innerCts.Token);

            Debug.Log("[Spawner] === SpawnLoop BEGIN ===");
            SpawnLoopAsync(linked.Token).Forget();
        }

        private async UniTaskVoid SpawnLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // ◆待機 ─────────────────
                    float wait = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
                    Debug.Log($"[Spawner] Wait {wait:F1}s");
                    await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);

                    // ◆プレイヤー取得
                    var player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
                    if (player == null) continue;

                    // ◆出現 ─────────────────
                    Debug.Log("[Spawner] Spawn");
                    await spawnManager.SetPositionNearPlayerAsync(player.transform.position, token);
                    await spawnManager.PlaySpawnedEffectAsync(token);
                    spawnManager.ActivateEnemy();
                    spawnManager.EnableChasing();

                    // ◆追跡 ─────────────────
                    float chase = UnityEngine.Random.Range(minChaseTime, maxChaseTime);
                    Debug.Log($"[Spawner] Chase {chase:F1}s");
                    await UniTask.Delay(TimeSpan.FromSeconds(chase), cancellationToken: token);

                    // ◆フェードアウト ────────
                    Debug.Log("[Spawner] Despawn");
                    await spawnManager.PlayDespawnedEffectAsync(token);
                    spawnManager.DisableChasing();
                    spawnManager.DeactivateEnemy();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[Spawner] SpawnLoop CANCELED");
                spawnManager.DisableChasing();
                spawnManager.DeactivateEnemy();
            }
            finally
            {
                _innerCts?.Dispose();
                _innerCts = null;
                Debug.Log("[Spawner] === SpawnLoop END ===");
            }
        }


        private async UniTaskVoid SpawnCycleAsync(CancellationToken token)
        {
            try
            {
                // ◆待機
                float wait = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
                Debug.Log($"[Spawner] Wait {wait:F1}s");
                await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);

                // ◆出現位置
                var player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
                if (player == null)
                {
                    Debug.LogWarning("[Spawner] Player not found");
                    return;
                }

                Debug.Log("[Spawner] SetPositionNearPlayerAsync");
                await spawnManager.SetPositionNearPlayerAsync(player.transform.position, token);

                // ◆出現エフェクト
                Debug.Log("[Spawner] PlaySpawnedEffectAsync");
                await spawnManager.PlaySpawnedEffectAsync(token);

                spawnManager.ActivateEnemy();
                spawnManager.EnableChasing();
                Debug.Log("[Spawner] Enemy Activated / Chase ON");

                // ◆追跡
                float chase = UnityEngine.Random.Range(minChaseTime, maxChaseTime);
                Debug.Log($"[Spawner] Chase {chase:F1}s");
                await UniTask.Delay(TimeSpan.FromSeconds(chase), cancellationToken: token);

                // ◆フェードアウト
                Debug.Log("[Spawner] PlayDespawnedEffectAsync");
                await spawnManager.PlayDespawnedEffectAsync(token);

                spawnManager.DisableChasing();
                spawnManager.DeactivateEnemy();
                Debug.Log("[Spawner] Enemy Deactivated");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[Spawner] SpawnCycle CANCELED");
                spawnManager.DisableChasing();
                spawnManager.DeactivateEnemy();
            }
            finally
            {
                _innerCts?.Dispose();
                _innerCts = null;
                Debug.Log("[Spawner] --- SpawnCycle END ---");
            }
        }

        /// <summary>
        /// 雑だけど怪異を部分的にフェードアウトさせるメソッド
        /// </summary>
        /// <param name="settings"></param>
        public async void ApFadeToPartialStatepear(PartialFadeSettings settings)
        {
            _despawnTimerCts?.Cancel();
            _despawnTimerCts?.Dispose();

            _despawnTimerCts = new CancellationTokenSource();
            var token = _despawnTimerCts.Token;
            if (settings != null)
            {
                var playTask = spawnManager.PlayDespawnedEffectLoopAsync(settings, token);

                // PlayDespawnedEffectAsync の完了を待ってから DisableChashing()
                await playTask;
            }
        }

        /// <summary>
        /// 部分フェードアウト状態になっている敵を、完全にフェードアウトさせる（消失させる）メソッド
        /// </summary>
        /// <param name="duration">完全フェードアウトにかかる時間</param>
        public async void ApCompletePartialFadeOut(float duration)
        {
            var playTask = spawnManager.PlayDespawnedEffectLoopEndAsync(duration);
            await playTask;
        }
    }
}
