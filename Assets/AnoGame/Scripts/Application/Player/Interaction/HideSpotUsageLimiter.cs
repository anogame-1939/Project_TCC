using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using AnoGame.Application.Enemy;
using AnoGame.Application.Direction;
using VContainer;
using System.Threading;

namespace AnoGame.Application.Player.Interaction
{
    /// <summary>
    /// ハイドスポットの連続使用を制限・ペナルティを与えるコンポーネント
    /// （最大数ランダム + 時間経過による回復）
    /// </summary>
    [RequireComponent(typeof(HideSpotZone))]
    public class HideSpotUsageLimiter : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("開始時の最大使用可能回数のランダム最小値")]
        [SerializeField] private int minRandomLimit = 1;
        [Tooltip("開始時の最大使用可能回数のランダム最大値")]
        [SerializeField] private int maxRandomLimit = 3;

        [Tooltip("1回分の使用回数が回復するまでの時間（秒）")]
        [SerializeField] private float recoveryInterval = 10.0f;

        [SerializeField] private float penaltyLockoutDuration = 30.0f;

        [SerializeField] private bool isActive = true;

        [Inject]
        private EnemySpawnManager _spawnManager;

        private HideSpotZone _hideSpot;

        // 現在の最大容量（ランダムで決定される）
        private int _currentMaxUsage;
        // 現在の残り使用回数
        private int _remainingUsage;

        private bool _isLocked;
        private CancellationTokenSource _recoveryCts;

        private void Awake()
        {
            _hideSpot = GetComponent<HideSpotZone>();
            RandomizeUsageLimit();
        }

        private void OnEnable()
        {
            MessageBroker.Default.Receive<HideBegan>()
                .Where(e => e.Spot == _hideSpot)
                .Subscribe(_ => OnHideBegan())
                .AddTo(this);
        }

        private void OnDisable()
        {
            StopRecovery();
        }

        /// <summary>
        /// 外部から使用回数を再設定・ランダマイズする
        /// </summary>
        public void RandomizeUsageLimit()
        {
            // Random.Range(int min, int max) is exclusive for max.
            // If we want 1 to 3 inclusive, we need Random.Range(1, 4).
            int nextMax = Random.Range(minRandomLimit, maxRandomLimit + 1);
            SetUsageLimit(nextMax);
        }

        /// <summary>
        /// 外部から使用回数を固定値で設定する
        /// </summary>
        public void SetUsageLimit(int count)
        {
            _currentMaxUsage = Mathf.Max(1, count);
            _remainingUsage = _currentMaxUsage;
            StopRecovery(); // Reset recovery state

            if (!_isLocked)
            {
                _hideSpot.SetInteractable(true);
            }

            Debug.Log($"[HideSpotUsageLimiter] Limit Set: {_currentMaxUsage} (Remaining: {_remainingUsage})");
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        private void OnHideBegan()
        {
            if (!isActive) return;
            if (_isLocked) return;

            // まず消費する
            _remainingUsage--;
            Debug.Log($"[HideSpotUsageLimiter] Used! Remaining: {_remainingUsage}/{_currentMaxUsage}");

            if (_remainingUsage == 0)
            {
                _hideSpot.SetInteractable(false);
            }

            // 0未満になったらペナルティ（つまり残り0の状態で入ったらアウト）
            if (_remainingUsage < 0)
            {
                ExecutePenalty();
                return;
            }

            // 回復タイマーが動いていなければ開始
            if (_recoveryCts == null)
            {
                StartRecovery();
            }
        }

        private void StartRecovery()
        {
            StopRecovery();
            _recoveryCts = new CancellationTokenSource();
            RecoveryRoutine(_recoveryCts.Token).Forget();
        }

        private void StopRecovery()
        {
            if (_recoveryCts != null)
            {
                _recoveryCts.Cancel();
                _recoveryCts.Dispose();
                _recoveryCts = null;
            }
        }

        private async UniTaskVoid RecoveryRoutine(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // 満タンなら終了
                if (_remainingUsage >= _currentMaxUsage)
                {
                    _remainingUsage = _currentMaxUsage;
                    _recoveryCts = null; // 自己終了
                    return;
                }

                await UniTask.Delay(System.TimeSpan.FromSeconds(recoveryInterval), cancellationToken: ct);

                _remainingUsage++;
                Debug.Log($"[HideSpotUsageLimiter] Recovered! Remaining: {_remainingUsage}/{_currentMaxUsage}");

                if (!_isLocked)
                {
                    _hideSpot.SetInteractable(true);
                }
            }
        }

        private void ExecutePenalty()
        {
            Debug.Log("[HideSpotUsageLimiter] Penalty Triggered!");
            // ペナルティなので残量は0にしておく
            _remainingUsage = 0;

            // ロック中も回復タイマーは動かす（ロック明けに少しでも回復しているように）
            StartRecovery();

            GameObject enemyObj = null;

            // 1. キラーを特定
            if (_spawnManager != null && _spawnManager.CurrentEnemyInstance != null)
            {
                enemyObj = _spawnManager.CurrentEnemyInstance;
            }
            // Fallback
            else if (EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.CurrentEnemyInstance != null)
            {
                enemyObj = EnemySpawnManager.Instance.CurrentEnemyInstance;
            }

            // 2. キラーのアクション実行
            if (enemyObj != null)
            {
                var coordinator = enemyObj.GetComponent<EnemyBehaviorCoordinator>();
                bool isChasing = (coordinator != null && coordinator.IsChasing);

                // ワープ (Chase中でなければ)
                if (!isChasing)
                {
                    var reaction = enemyObj.GetComponent<EnemyTeleportReaction>();
                    if (reaction != null)
                    {
                        if (!_hideSpot.IsEnemyNear(enemyObj.transform.position))
                        {
                            reaction.CancelDisableTimer();
                            reaction.WarpToNearPlayerSplinePoint();
                        }
                        else
                        {
                            Debug.Log("[HideSpotUsageLimiter] Enemy is already near. Skip Warp.");
                        }
                    }
                }
                else
                {
                    Debug.Log("[HideSpotUsageLimiter] Enemy is already Chasing. Skip Warp.");
                }

                // Chaseモードへ移行
                if (coordinator != null)
                {
                    coordinator.NotifyFound();
                }
            }

            // 3. 強制的に追い出す
            _hideSpot.ForceExitHideAsync().Forget();

            // 4. ロックダウン
            LockSpot(penaltyLockoutDuration).Forget();
        }

        private async UniTaskVoid LockSpot(float duration)
        {
            _isLocked = true;
            _hideSpot.SetInteractable(false);
            Debug.Log($"[HideSpotUsageLimiter] Spot Locked for {duration}s");

            await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());

            _isLocked = false;
            _hideSpot.SetInteractable(true);
            Debug.Log("[HideSpotUsageLimiter] Spot Unlocked");
        }

#if UNITY_EDITOR
        [ContextMenu("Randomize Limit")]
        private void DebugRandomize()
        {
            RandomizeUsageLimit();
        }

        [ContextMenu("Execute Penalty")]
        private void DebugPenalty()
        {
            ExecutePenalty();
        }
#endif
    }
}
