using System;
using System.Collections;
using System.Threading;
using AnoGame.Application.Objects;
using AnoGame.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnoGame.Application.Animation.Gmmicks
{
    public class TrapController : MonoBehaviour
    {
        [SerializeField] GameObject _trapObject;
        [SerializeField] Animator _animator;
        [SerializeField] ParticleSystem _particleObject;
        [SerializeField] Vector3 _offsetPosition;

        [Header("タイミング")]
        [SerializeField, Min(0f)] float _stopDuration = 1.0f;        // 可視維持
        [SerializeField, Min(0f)] float _disappearDuration = 0.35f;  // 未使用なら0でOK

        [Header("フェード")]
        [SerializeField, Min(0f)] float _fadeIn = 0.2f;
        [SerializeField, Min(0f)] float _fadeOut = 0.25f;
        [SerializeField] SpriteFader2D _fader;

        [Header("後処理")]
        [SerializeField] bool _hideBySetActive = false;        // フェードアウト後に非アク化する
        [SerializeField] bool _resetToInitialAfterHide = true; // 完了後に初期位置へ戻す

        [Header("固定配置・再出現")]
        [Tooltip("シーン上に固定配置し、起動前から表示しておきたい場合にON")]
        [SerializeField] bool _startVisible = false;
        [Tooltip("起動が終わったらクールダウン後に自動で再出現する（固定配置向け）")]
        [SerializeField] bool _autoReappear = false;
        [Tooltip("再出現までのクールダウン秒数")]
        [SerializeField, Min(0f)] float _reappearCooldown = 2.0f;
        [Tooltip("再出現する座標を初期位置に固定（OFFなら直近の出現位置）")]
        [SerializeField] bool _reappearAtInitialPosition = true;

        private const string ANIM_IS_APPEAR = "IsAppear";
        private Vector3 _initialPos;
        private Vector3 _lastAppearPos;

        // Busy / 実行状態
        private int _busyRef;                       // 参照カウント式 Busy（フェード/本編/再出現待ち）
        public bool IsBusy => _busyRef > 0;
        private bool _actionRunning;                // RunTrapAsync 実行中
        private CancellationTokenSource _fadeCts;   // フェード用 CTS
        private CancellationTokenSource _reappearCts; // クールダウン再出現用 CTS

        void Awake()
        {
            if (_trapObject == null) _trapObject = gameObject;
            if (_animator == null) _animator = GetComponentInChildren<Animator>(true);
            if (_fader == null) _fader = GetComponentInChildren<SpriteFader2D>(true);

            // 初期は透明（Startで _startVisible を反映）
            _fader?.SetAlphaImmediate(0f);
        }

        void Start()
        {
            _initialPos = _trapObject.transform.position;

            if (_particleObject != null) _particleObject.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _animator?.SetBool(ANIM_IS_APPEAR, false);

            if (_startVisible)
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                if (!_trapObject.activeSelf) _trapObject.SetActive(true);
                _fader?.SetAlphaImmediate(1f); // 最初から表示
            }
            else
            {
                if (_hideBySetActive) _trapObject.SetActive(false); // 非表示Start派
            }
        }

        // ===== Spawner/外部 からのAPI =====

        /// <summary>指定座標でフェードインして現れる（Spawner向け）</summary>
        public void AppearAt(Vector3 worldPos) => _ = AppearAtAsync(worldPos);

        /// <summary>初期座標でフェードインして現れる（固定配置/再出現向け）</summary>
        public void Appear() => _ = AppearAtAsync(_initialPos);

        /// <summary>フェードアウトして消す（必要なら即時）</summary>
        public void Disappear(bool immediate = false)
        {
            // 本編中は外部要求を基本無視（責務が衝突するため）
            if (_actionRunning)
            {
                if (immediate) ForceHideImmediate();
                return;
            }

            if (immediate) ForceHideImmediate();
            else _ = DisappearAsync();
        }

        // ===== 内部実装 =====

        private async UniTask AppearAtAsync(Vector3 worldPos)
        {
            // 本編中は位置のみ更新（可視化は本編が実施）
            if (_actionRunning)
            {
                _trapObject.transform.position = worldPos + _offsetPosition;
                _lastAppearPos = _trapObject.transform.position;
                return;
            }

            CancelReappear(); // 予定されていた再出現をキャンセル

            RestartFadeCts(out var ct);
            BusyEnter();
            try
            {
                _fader?.SetAlphaImmediate(0f); // 可視化前にα=0を保証
                _trapObject.transform.position = worldPos + _offsetPosition;
                _lastAppearPos = _trapObject.transform.position;

                if (!gameObject.activeSelf) gameObject.SetActive(true);
                if (!_trapObject.activeSelf) _trapObject.SetActive(true);

                if (_fader != null) await _fader.FadeIn(_fadeIn, ct);
            }
            catch (OperationCanceledException) { }
            finally { BusyExit(); }
        }

        private async UniTask DisappearAsync()
        {
            CancelReappear();

            RestartFadeCts(out var ct);
            BusyEnter();
            try
            {
                if (_fader != null) await _fader.FadeOut(_fadeOut, ct);

                if (_hideBySetActive) _trapObject.SetActive(false);
                if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
            }
            catch (OperationCanceledException) { }
            finally
            {
                BusyExit();
                MaybeScheduleReappear(); // 外部消灯でも自動再出現したい場合に対応
            }
        }

        private void ForceHideImmediate()
        {
            BusyEnter();
            try
            {
                CancelReappear();
                _fader?.SetAlphaImmediate(0f);
                if (_hideBySetActive) _trapObject.SetActive(false);
                if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
            }
            finally { BusyExit(); }
        }

        private void MaybeScheduleReappear()
        {
            if (!_autoReappear) return;
            CancelReappear();

            _reappearCts = new CancellationTokenSource();
            var ct = _reappearCts.Token;

            BusyEnter();                        // クールダウン待ちも Busy に
            _ = ReappearAfterCooldown(ct).ContinueWith(() => BusyExit());
            _ = ReappearAfterCooldown(ct);      // 後始末は中で finally がやる
        }

        private async UniTask ReappearAfterCooldown(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_reappearCooldown), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);

                var pos = _reappearAtInitialPosition ? _initialPos : _lastAppearPos;
                await AppearAtAsync(pos); // 再度フェードイン
            }
            catch (OperationCanceledException) { /* シーン破棄・再命令 */ }
            finally { BusyExit(); }  
        }

        private void CancelReappear()
        {
            _reappearCts?.Cancel();
            _reappearCts?.Dispose();
            _reappearCts = null;
        }

        private void RestartFadeCts(out CancellationToken ct)
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = new CancellationTokenSource();
            ct = _fadeCts.Token;
        }

        private void BusyEnter() => _busyRef++;
        private void BusyExit()  => _busyRef = Mathf.Max(0, _busyRef - 1);

        // ===== トリガーで発火する「罠の本編」 =====

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(SLFBRules.TAG_PLAYER)) return;
            if (_actionRunning) return; // 再入防止

            _ = RunTrapAsync(other.transform, this.GetCancellationTokenOnDestroy());
        }

        private async UniTaskVoid RunTrapAsync(Transform target, CancellationToken ct)
        {
            _actionRunning = true;
            BusyEnter();
            try
            {
                _fadeCts?.Cancel();
                CancelReappear();

                _fader?.SetAlphaImmediate(0f); // パッと出ない

                if (!gameObject.activeSelf) gameObject.SetActive(true);
                if (!_trapObject.activeSelf) _trapObject.SetActive(true);

                _trapObject.transform.position = target.position + _offsetPosition;
                _lastAppearPos = _trapObject.transform.position;

                _animator?.SetBool(ANIM_IS_APPEAR, true);
                if (_particleObject != null )_particleObject.Play();

                if (_fader != null) await _fader.FadeIn(_fadeIn, ct);

                await UniTask.Delay(TimeSpan.FromSeconds(_stopDuration), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);

                _animator?.SetBool(ANIM_IS_APPEAR, false);
                if (_particleObject != null ) _particleObject.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                if (_fader != null) await _fader.FadeOut(_fadeOut, ct);

                if (_hideBySetActive) _trapObject.SetActive(false);
                if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
            }
            catch (OperationCanceledException) { }
            finally
            {
                BusyExit();
                _actionRunning = false;
                MaybeScheduleReappear(); // 本編終了後の自動再出現
            }
        }
    }
}
