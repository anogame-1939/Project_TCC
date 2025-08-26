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

        private Vector3 _initialPos;
        private const string ANIM_IS_APPEAR = "IsAppear";
        private bool _busy;                                          // 発動中ガード

        // Spawner から“再生中は消さない”判定に使えるよう公開
        public bool IsBusy => _busy;

        void Awake()
        {
            if (_trapObject == null) _trapObject = gameObject;
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_fader == null) _fader = GetComponentInChildren<SpriteFader2D>(true);
        }

        void Start()
        {
            _initialPos = _trapObject.transform.position;
            if (_particleObject != null)
                _particleObject.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_animator != null)
                _animator.SetBool(ANIM_IS_APPEAR, false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_busy) return;
            if (!other.CompareTag(SLFBRules.TAG_PLAYER)) return;

            StartCoroutine(CoRunTrap(other.transform));
            _ = RunTrapAsync(other.transform, this.GetCancellationTokenOnDestroy());
        }

        private async UniTaskVoid RunTrapAsync(Transform target, CancellationToken ct)
        {
            try
            {
                _busy = true;

                // 位置セット & 表示
                _trapObject.transform.position = target.position + _offsetPosition;
                if (!_trapObject.activeSelf) _trapObject.SetActive(true);

                _animator?.SetBool(ANIM_IS_APPEAR, true);
                _particleObject?.Play();

                // フェードイン
                if (_fader != null) await _fader.FadeIn(_fadeIn, ct);

                // 見せておく
                await UniTask.Delay(TimeSpan.FromSeconds(_stopDuration), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);

                // 消えるアニメへ
                _animator?.SetBool(ANIM_IS_APPEAR, false);
                _particleObject?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                // フェードアウト完了を待ってから後処理
                if (_fader != null) await _fader.FadeOut(_fadeOut, ct);

                // 完全に見えなくなってから非表示＆位置リセット
                _trapObject.SetActive(false);
                _trapObject.transform.position = _initialPos;
            }
            catch (OperationCanceledException) { /* scene unload 等 */ }
            finally
            {
                _busy = false;
            }
        }

        private IEnumerator CoRunTrap(Transform target)
        {
            _busy = true;

            // 表示位置を決定（＋任意のオフセット）
            _trapObject.transform.position = target.position + _offsetPosition;

            // 出現
            _animator.SetBool(ANIM_IS_APPEAR, true);
            if (_particleObject != null) _particleObject.Play();

            // しばらく見せる
            yield return new WaitForSeconds(_stopDuration);

            // 消えるアニメを開始
            _animator.SetBool(ANIM_IS_APPEAR, false);
            if (_particleObject != null)
                _particleObject.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // 消えるアニメが終わるまで待機（最もシンプルな方式）
            yield return new WaitForSeconds(_disappearDuration);

            // 完了後に非表示＆初期位置へ戻す（必要なら）
            if (_hideBySetActive) _trapObject.SetActive(false);
            if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;

            _busy = false;
        }

        // --- もしアニメーションイベントを使うなら ---
        // 消えるクリップの最後にこのイベントを呼べば、_disappearDuration を 0 にしてもOK
        public void AnimationEvent_DisappearComplete()
        {
            if (!_busy) return;

            if (_hideBySetActive) _trapObject.SetActive(false);
            if (_resetToInitialAfterHide) _trapObject.transform.position = _initialPos;

            _busy = false;
        }
    }
}
