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
            spawnManager.EnableChaising();
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
                spawnManager.DisableChashing();
                Debug.Log("逃げ切り成功");

                // 敵がまだ生きているか確認
                if (spawnManager?.CurrentEnemyInstance == null) return;

                // フェードアウト演出開始
                var playTask = spawnManager.PlayDespawnedEffectAsync(token);

                // 1 秒後に DeactivateEemy() を実行
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
                    spawnManager.DeactivateEemy();
                });

                // エフェクト完了を待つ
                await playTask;

                // 念のため Deactivate
                
                spawnManager.DeactivateEemy();

                // EventDataを成功とする
                spawnManager.ScuccesEvent();
                
            }
            catch (OperationCanceledException)
            {
                Debug.Log("逃げ切り失敗");
                // タイマーが途中でキャンセルされた場合は何もしない
            }
        }





        public void RandomSpawnLoop()
        {
            spawnManager.SetupToRamdomMode();
            _spawnLoopCts = new CancellationTokenSource();
            RandomSpawnLoopAsync(_spawnLoopCts.Token).Forget();
        }

        public async void CancelRandomSpawnLoop()
        {
            spawnManager.DisableChashing();

            _spawnLoopCts.Cancel();
            _spawnLoopCts = new CancellationTokenSource();
            var token = _spawnLoopCts.Token;

            var playTask = spawnManager.PlayDespawnedEffectAsync(token);
            await playTask;
            
        }


        /// <summary>
        /// 外部から呼び出す敵生成開始メソッド
        /// </summary>
        public void TriggerEnemySpawnToStory()
        {
            Debug.LogError("廃止");
        }

        /// <summary>
        /// Gameplay 中のみランダムに敵をスポーンし、
        /// さらにランダム時間だけチェイスさせたら Deactivate する非同期ループ
        /// </summary>
        private async UniTaskVoid RandomSpawnLoopAsync(CancellationToken token)
        {
            void OnStateChanged(GameState s)
            {
                // TODO:ゲームオーバーではなく会話シーンに突入したときにする。ゲームオーバー時の敵削除は別でやる
                if (s == GameState.GameOver) _spawnLoopCts?.Cancel();
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
                    if (GameStateManager.Instance.CurrentSubState == GameSubState.Safety) continue;

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

                // spawnManager.DestroyCurrentEnemyInstance();
                spawnManager.DisableChashing();
                spawnManager.DeactivateEemy();

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
