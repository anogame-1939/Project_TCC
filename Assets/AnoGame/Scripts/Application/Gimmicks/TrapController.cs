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
        [SerializeField, Min(0f)] float _stopDuration = 1.0f;        // 見せておく時間（従来どおり）
        [SerializeField, Min(0f)] float _disappearDuration = 0.35f;  // 消えるアニメの長さ分の待機

        [Header("フェード")]
        [SerializeField, Min(0f)] float _fadeIn = 0.2f;
        [SerializeField, Min(0f)] float _fadeOut = 0.25f;
        [SerializeField] SpriteFader2D _fader;   // ここに上のコンポーネントを割り当て
        
        [Header("後処理")]
        [SerializeField] bool _hideBySetActive = false;              // 必要なら SetActive(false) まで行う
        [SerializeField] bool _resetToInitialAfterHide = true;       // 完了後に初期位置へ戻す

        private const string ANIM_IS_APPEAR = "IsAppear";
        private Vector3 _initialPos;
        private int _busyRef;                       // 参照カウント式 Busy
        public bool IsBusy => _busyRef > 0;         // Spawner が見るプロパティ
        private bool _actionRunning;                // RunTrapAsync 実行中
        private CancellationTokenSource _fadeCts;   // フェードのキャンセル

        void Awake()
        {
            if (_trapObject == null) _trapObject = gameObject;
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_fader == null) _fader = GetComponentInChildren<SpriteFader2D>(true);

            // ★ 初期で完全透明（GameObjectが inactive でもOK）
            _fader?.SetAlphaImmediate(0f);
        }

        void Start()
        {
            _initialPos = _trapObject.transform.position;
            if (_particleObject != null)
                _particleObject.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_animator != null)
                _animator.SetBool(ANIM_IS_APPEAR, false);

            if (_hideBySetActive) _trapObject.SetActive(false);
        }

        public void AppearAt(Vector3 worldPos)
        {
            _ = AppearAtAsync(worldPos);
        }

        public void Disappear(bool immediate = false)
        {
            // RunTrap（本編）中は Spawner 要求を無視：本編が責務を持つ
            if (_actionRunning)
            {
                if (immediate)
                {
                    // 即時だけは尊重（緊急停止用途）
                    _fader?.SetAlphaImmediate(0f);
                    if (_hideBySetActive) _trapObject.SetActive(false);
                    if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
                }
                return;
            }

            if (immediate)
            {
                BusyEnter();
                try
                {
                    _fader?.SetAlphaImmediate(0f);
                    if (_hideBySetActive) _trapObject.SetActive(false);
                    if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
                }
                finally { BusyExit(); }
            }
            else
            {
                _ = DisappearAsync();
            }
        }

        private async UniTask AppearAtAsync(Vector3 worldPos)
        {
            if (_actionRunning)
            {
                // 本編中は位置だけ更新
                _trapObject.transform.position = worldPos + _offsetPosition;
                return;
            }

            RestartFadeCts(out var ct);
            BusyEnter();
            try
            {
                // ★ 先にα=0へ（まだ非表示のまま）
                _fader?.SetAlphaImmediate(0f);

                // 位置→有効化→フェードイン
                _trapObject.transform.position = worldPos + _offsetPosition;

                // 親・子どちらも起こす（この時点で既にα=0）
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                if (!_trapObject.activeSelf) _trapObject.SetActive(true);

                if (_fader != null) await _fader.FadeIn(_fadeIn, ct);
            }
            finally { BusyExit(); }
        }

        private async UniTask DisappearAsync()
        {
            RestartFadeCts(out var ct);

            BusyEnter();
            try
            {
                if (_fader != null) await _fader.FadeOut(_fadeOut, ct);

                if (_hideBySetActive) _trapObject.SetActive(false);
                if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
            }
            catch (OperationCanceledException) { /* フェード競合など */ }
            finally { BusyExit(); }
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

                // ★ 先にα=0（パッと出ない）
                _fader?.SetAlphaImmediate(0f);

                // 位置→有効化
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                if (!_trapObject.activeSelf) _trapObject.SetActive(true);
                _trapObject.transform.position = target.position + _offsetPosition;

                _animator?.SetBool(ANIM_IS_APPEAR, true);
                _particleObject?.Play();

                // フェードイン
                if (_fader != null) await _fader.FadeIn(_fadeIn, ct);

                await UniTask.Delay(TimeSpan.FromSeconds(_stopDuration), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);

                _animator?.SetBool(ANIM_IS_APPEAR, false);
                _particleObject?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                // フェードアウト完了を待ってから後処理
                if (_fader != null) await _fader.FadeOut(_fadeOut, ct);

                if (_hideBySetActive) _trapObject.SetActive(false);
                if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;
            }
            finally
            {
                BusyExit();
                _actionRunning = false;
            }
        }
    }
}
