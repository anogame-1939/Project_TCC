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

        /// <summary>
        // エントリ（状態監視）用 CTS
        private CancellationTokenSource _spawnEntryCts;

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

        /// <summary>
        /// エントリ：常に「スポーン可能になるのを待って → SpawnLoop 起動 → 不可能になったらキャンセル → 待機…」を繰り返す
        /// </summary>
        public void RandomSpawnLoop()
        {
            _spawnEntryCts?.Cancel();
            _spawnEntryCts?.Dispose();
            _spawnLoopCts?.Cancel();
            _spawnLoopCts?.Dispose();

            _spawnEntryCts = new CancellationTokenSource();
            _ = RandomSpawnEntryAsync(_spawnEntryCts.Token);
        }

        private async UniTaskVoid RandomSpawnEntryAsync(CancellationToken entryToken)
        {
            try
            {
                while (!entryToken.IsCancellationRequested)
                {
                    // —— スポーン可能状態になるまで待機 —— 
                    await UniTask.WaitUntil(
                        () => IsSpawnableState(),
                        cancellationToken: entryToken
                    );

                    // SpawnLoop 用 CTS を作り直し（EntryToken とリンク）
                    _spawnLoopCts?.Cancel();
                    _spawnLoopCts?.Dispose();
                    _spawnLoopCts = CancellationTokenSource
                        .CreateLinkedTokenSource(entryToken);

                    try
                    {
                        // —— 実際の敵生成ループを起動 —— 
                        await RandomSpawnLoopAsync(_spawnLoopCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.Log("▶ SpawnLoop がキャンセルされました");
                    }
                    // スポーン不可能になったらここに戻り、再び待機
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("▶ EntryLoop がキャンセルされました");
            }
        }

        /// <summary>
        /// 実際のスポーン処理：内部の continue チェックは不要に。
        /// 不可能状態への遷移は OnStateChanged/OnSubStateChanged で検知してキャンセルします。
        /// </summary>
        private async UniTask RandomSpawnLoopAsync(CancellationToken token)
        {
            // GameState の変化でキャンセル
            void OnStateChanged(GameState s)
            {
                if (s == GameState.InGameEvent || s == GameState.GameOver)
                {
                    _spawnLoopCts?.Cancel();
                }
            }
            // GameSubState の変化でキャンセル
            void OnSubStateChanged(GameSubState ss)
            {
                if (ss == GameSubState.Safety)
                {
                    _spawnLoopCts?.Cancel();
                }
            }

            GameStateManager.Instance.OnStateChanged += OnStateChanged;
            GameStateManager.Instance.OnSubStateChanged += OnSubStateChanged;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // —— ここから元のランダムスポーン処理をそのまま —— 

                    // 1 フレーム待って
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    // 待機時間ランダム
                    float waitTime = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                    // プレイヤー付近にセット
                    var player = GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
                    if (player == null) continue;
                    await spawnManager.SetPositionNearPlayerAsync(player.transform, token);

                    // インゲーム、インベントリ/オプション開いているときはスポーンをスキップ
                    if (GameStateManager.Instance.CurrentState == GameState.InGameEvent
                        || GameStateManager.Instance.CurrentState == GameState.Inventory
                        || GameStateManager.Instance.CurrentState == GameState.Settings)
                    {
                        continue;
                    }

                    // 出現演出
                    await spawnManager.PlaySpawnedEffectAsync(token);

                    // 本体出現＆チェイス
                    spawnManager.ActivateEnemy();
                    spawnManager.EnableChasing();

                    // チェイス時間ランダム
                    float chaseTime = UnityEngine.Random.Range(minChaseTime, maxChaseTime);
                    await UniTask.Delay(TimeSpan.FromSeconds(chaseTime), cancellationToken: token);

                    // 退場演出
                    var playTask = spawnManager.PlayDespawnedEffectAsync(token);
                    UniTask.Void(async () =>
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                        spawnManager.DeactivateEnemy();
                    });
                    await playTask;

                    // 後片付け
                    spawnManager.DisableChasing();
                    spawnManager.DeactivateEnemy();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("▶ RandomSpawnLoopAsync: キャンセル検知");

                // ── 退場演出を再生 ──
                var effectToken = CancellationToken.None;
                try
                {
                    await spawnManager.PlayDespawnedEffectAsync(effectToken);
                }
                catch (OperationCanceledException)
                {
                    // ここは基本通らないはずです
                }

                // ── 演出後に後処理 ──
                spawnManager.DisableChasing();
                spawnManager.DeactivateEnemy();
            }
            finally
            {
                GameStateManager.Instance.OnStateChanged   -= OnStateChanged;
                GameStateManager.Instance.OnSubStateChanged -= OnSubStateChanged;
            }
        }

        /// <summary>
        /// スポーン可能かどうかを判定する共通ロジック
        /// </summary>
        private bool IsSpawnableState()
        {
            var state    = GameStateManager.Instance.CurrentState;
            var subState = GameStateManager.Instance.CurrentSubState;
            bool okState = state == GameState.Gameplay
                        || state == GameState.Inventory
                        || state == GameState.Settings;
            return okState && subState != GameSubState.Safety;
        }

        /// <summary>
        /// 外部から完全に止めるとき
        /// </summary>
        public async void CancelRandomSpawnLoop()
        {
            _spawnLoopCts?.Cancel();
            _spawnEntryCts?.Cancel();

            spawnManager.DisableChasing();
            await spawnManager.PlayDespawnedEffectAsync(
                _spawnLoopCts?.Token ?? CancellationToken.None
            );
            spawnManager.DeactivateEnemy();
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
