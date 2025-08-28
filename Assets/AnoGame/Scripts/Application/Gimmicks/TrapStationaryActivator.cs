using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnoGame.Application.Gmmicks
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class TrapStationaryActivator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private TrapController[] _traps;
        [SerializeField] private string _playerTag = "Player";

        [Header("発動タイミング")]
        [Tooltip("トリガー進入後、出現させるまでの待ち秒数（例：10秒なら 10f）")]
        [SerializeField, Min(0f)] private float _requiredSeconds = 10f;

        [Header("離脱時の後処理")]
        [Tooltip("トリガーから出てからフェードアウトするまでの遅延秒数")]
        [SerializeField, Min(0f)] private float _fadeOutDelayOnExit = 0.25f;

        [Header("起動時の初期化")]
        [Tooltip("開始時に全トラップを SetActive(false) にする")]
        [SerializeField] private bool _deactivateTrapsOnStart = true;

        // 内部状態
        private bool _inside;
        private bool _appearedThisStay; // 入室→一度出したら再カウントしない（退室まで）
        private CancellationTokenSource _appearCts;
        private CancellationTokenSource _fadeOutCts;
        private Collider _col; // キャッシュ

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            _col = GetComponent<Collider>();
            if (_col && !_col.isTrigger)
            {
                Debug.LogWarning($"[{nameof(TrapStationaryActivator)}] Collider.isTrigger を true にします。");
                _col.isTrigger = true;
            }
        }

        private void Start()
        {
            if (_deactivateTrapsOnStart) DeactivateAllImmediate();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;

            _inside = true;
            _appearedThisStay = false;

            CancelFadeOut();         // 退室ディレイ中なら中止
            StartAppearCountdown();  // 入室時点から秒カウント
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;

            _inside = false;
            _appearedThisStay = false;

            CancelAppearCountdown(); // カウント破棄
            ScheduleFadeOut();       // 退室ディレイ後にフェード→非アク化
        }

        // ===== 発動カウントダウン =====
        private void StartAppearCountdown()
        {
            CancelAppearCountdown();
            _appearCts = new CancellationTokenSource();
            var ct = _appearCts.Token;
            _ = AppearAfterDelay(ct);
        }

        private void CancelAppearCountdown()
        {
            _appearCts?.Cancel();
            _appearCts?.Dispose();
            _appearCts = null;
        }

        private async UniTaskVoid AppearAfterDelay(CancellationToken ct)
        {
            try
            {
                if (_requiredSeconds > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_requiredSeconds),
                                        DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
                }

                // まだトリガー内で、今回の滞在中に未出現なら出す
                if (_inside && !_appearedThisStay)
                {
                    await AppearAllWithActivateIfNotBusy(ct);
                    _appearedThisStay = true;
                }
            }
            catch (OperationCanceledException) { /* 入退室や破棄での中断 */ }
        }

        // ===== 出現/消失（SetActive 管理込み） =====
        private async UniTask AppearAllWithActivateIfNotBusy(CancellationToken ct)
        {
            if (_traps == null || _traps.Length == 0) return;

            foreach (var trap in _traps)
            {
                if (trap == null) continue;

                // もし消えていても確実に起動させる
                if (!trap.gameObject.activeSelf)
                    trap.gameObject.SetActive(true);

                // Disappear 中（Busy）なら終わるまで待ってから出現させる
                while (trap.IsBusy && !ct.IsCancellationRequested)
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (!ct.IsCancellationRequested)
                {
                    trap.Appear(); // フェードイン開始
                }
            }
        }

        private async UniTask DisappearAllThenDeactivate(CancellationToken ct)
        {
            if (_traps == null || _traps.Length == 0) return;

            // まず全てにフェードアウト指示
            foreach (var trap in _traps)
            {
                if (trap == null) continue;
                if (trap.gameObject.activeSelf)
                    trap.Disappear(); // フェードアウト開始
            }

            // すべてのフェードが終わるのを待つ
            bool anyBusy;
            do
            {
                anyBusy = false;
                foreach (var trap in _traps)
                {
                    if (trap == null) continue;
                    if (trap.IsBusy) { anyBusy = true; break; }
                }
                if (anyBusy)
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            while (anyBusy && !ct.IsCancellationRequested);

            // 完了後に一括で非アクティブ化
            foreach (var trap in _traps)
            {
                if (trap == null) continue;
                if (trap.gameObject.activeSelf)
                    trap.gameObject.SetActive(false);
            }
        }

        private void DeactivateAllImmediate()
        {
            if (_traps == null || _traps.Length == 0) return;
            foreach (var trap in _traps)
            {
                if (trap == null) continue;
                if (trap.gameObject.activeSelf)
                    trap.gameObject.SetActive(false);
            }
        }

        // ===== 退室ディレイ管理 =====
        private void ScheduleFadeOut()
        {
            CancelFadeOut();
            _fadeOutCts = new CancellationTokenSource();
            var ct = _fadeOutCts.Token;
            _ = FadeOutAfterDelay(ct);
        }

        private void CancelFadeOut()
        {
            _fadeOutCts?.Cancel();
            _fadeOutCts?.Dispose();
            _fadeOutCts = null;
        }

        private async UniTaskVoid FadeOutAfterDelay(CancellationToken ct)
        {
            try
            {
                if (_fadeOutDelayOnExit > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_fadeOutDelayOnExit),
                                        DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
                }
                await DisappearAllThenDeactivate(ct);
            }
            catch (OperationCanceledException) { }
        }

        private void OnDisable()
        {
            CancelAppearCountdown();
            CancelFadeOut();
        }
    }
}
