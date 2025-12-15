using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using AnoGame.Application.Enemy;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.Direction;
using VContainer;

namespace AnoGame.Application.Player.Interaction
{
    /// <summary>
    /// ハイドスポットの連続使用を制限・ペナルティを与えるコンポーネント
    /// </summary>
    [RequireComponent(typeof(HideSpotZone))]
    public class HideSpotUsageLimiter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxUsageCount = 3;
        [SerializeField] private float usageResetTime = 60.0f; // 未使用ならリセットする機能があってもいいが、要望は「3回」なのでシンプルに
        [SerializeField] private float penaltyLockoutDuration = 30.0f;

        // [Header("References")]
        // [SerializeField] private EnemyTeleportReaction enemyReaction; // Injectするので削除orコメントアウト

        [Inject]
        private EnemySpawnManager _spawnManager;

        private HideSpotZone _hideSpot;
        private int _currentUsage;
        private System.IDisposable _resetTimer;
        private bool _isLocked;

        private void Awake()
        {
            _hideSpot = GetComponent<HideSpotZone>();
        }

        private void OnEnable()
        {
            MessageBroker.Default.Receive<HideBegan>()
                .Where(e => e.Spot == _hideSpot)
                .Subscribe(_ => OnHideBegan())
                .AddTo(this);
        }

        private void OnHideBegan()
        {
            if (_isLocked) return; // 本来はInteractできないはずだが念のため

            _currentUsage++;
            Debug.Log($"[HideSpotUsageLimiter] Usage: {_currentUsage}/{maxUsageCount}");

            if (_currentUsage >= maxUsageCount)
            {
                ExecutePenalty();
            }
        }

        private void ExecutePenalty()
        {
            Debug.Log("[HideSpotUsageLimiter] Penalty Triggered!");
            _currentUsage = 0; // カウンタはリセット

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
                // ワープ
                var reaction = enemyObj.GetComponent<EnemyTeleportReaction>();
                if (reaction != null)
                {
                    reaction.CancelDisableTimer();
                    reaction.WarpToNearPlayerSplinePoint();
                }

                // [NEW] Chaseモードへ移行
                var coordinator = enemyObj.GetComponent<EnemyBehaviorCoordinator>();
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
            _hideSpot.SetInteractable(false); // HideSpotZone に SetInteractable が必要
            Debug.Log($"[HideSpotUsageLimiter] Spot Locked for {duration}s");

            await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());

            _isLocked = false;
            _hideSpot.SetInteractable(true);
            Debug.Log("[HideSpotUsageLimiter] Spot Unlocked");
        }
    }
}
