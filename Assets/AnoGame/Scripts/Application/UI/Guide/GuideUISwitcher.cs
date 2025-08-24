using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using AnoGame.Application; // ★ 直接参照

namespace AnoGame.UI.Guide
{
    public class GuideUISwitcher : MonoBehaviour
    {
        [Header("UI Images")]
        [SerializeField] private Image padImage;
        [SerializeField] private Image keyboardImage;

        [Header("Timings (seconds)")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float defaultHoldSeconds = 5f;
        [SerializeField] private float fadeOutDuration = 0.25f;

        [Header("Options")]
        [SerializeField] private bool useUnscaledTime = true; // 一時停止中も動かすなら true

        private PlayerInput _playerInput;
        private string _lastScheme;

        private CanvasGroup _padGroup;
        private CanvasGroup _keyboardGroup;

        private CancellationTokenSource _lifetimeCts;  // Destroyまで有効
        private CancellationTokenSource _sequenceCts;  // 通常ヒント（タイマーあり）
        private CancellationTokenSource _stickyCts;    // AFK中のSticky（出しっぱなし）
        private UniTask _stickyTask;

        private bool SequenceActive => _sequenceCts != null && !_sequenceCts.IsCancellationRequested;
        private bool StickyActive   => _stickyCts   != null && !_stickyCts.IsCancellationRequested;

        void Awake()
        {
            _lifetimeCts = new CancellationTokenSource();
        }

        void Start()
        {
            // PlayerInput 取得
            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput がシーンに見つかりません。");
                enabled = false;
                return;
            }

            _lastScheme = _playerInput.currentControlScheme;

            // CanvasGroup 準備
            _padGroup = EnsureCanvasGroup(padImage);
            _keyboardGroup = EnsureCanvasGroup(keyboardImage);
            _padGroup.blocksRaycasts = false;
            _padGroup.interactable = false;
            _keyboardGroup.blocksRaycasts = false;
            _keyboardGroup.interactable = false;

            // 初期は非表示
            HideImmediate(_padGroup);
            HideImmediate(_keyboardGroup);

            // 起動時に現在のデバイスガイドを5秒表示（Gameplay中のみ）
            // ShowHint(defaultHoldSeconds).Forget();
        }

        void Update()
        {
            if (_playerInput == null) return;

            // ★ Gameplay外に出たら、進行中の表示を全て中断＆即非表示
            if (!IsGameplayNow() && (SequenceActive || StickyActive))
            {
                CancelAndHideAll();
                return;
            }

            // スキーム変化を監視
            var currentScheme = _playerInput.currentControlScheme;
            if (currentScheme != _lastScheme)
            {
                _lastScheme = currentScheme;

                if (StickyActive)
                {
                    // ★ Sticky中はフェードせずに表示内容だけ差し替え（チラつき防止）
                    ForceMatchStickyGroupToScheme();
                }
                else
                {
                    // 通常ヒント（内部でGameplayチェック）
                    ShowHint(defaultHoldSeconds).Forget();
                }
            }
        }

        void OnDestroy()
        {
            _sequenceCts?.Cancel();
            _sequenceCts?.Dispose();
            _stickyCts?.Cancel();
            _stickyCts?.Dispose();
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
        }

        /// <summary>外部から即座に消したい場合</summary>
        public void HideHintImmediate()
        {
            CancelAndHideAll();
        }

        // === 公開API：外部（AFK検知など）から呼べる ===

        /// <summary>通常ヒント（フェードイン→保持→フェードアウト）。Gameplay外 or Sticky中はスキップ。</summary>
        public UniTask ShowHint(float seconds = 5f)
        {
            if (!IsGameplayNow()) return UniTask.CompletedTask;
            if (StickyActive)     return UniTask.CompletedTask; // AFK中は通常ヒントを出さない
            return ShowCurrentDeviceHintSequenceAsync(seconds, _lifetimeCts.Token);
        }

        public void ShowSticky()
        {
            if (!IsGameplayNow()) return;

            // 進行中の通常ヒントは中断
            _sequenceCts?.Cancel();
            _sequenceCts?.Dispose();
            _sequenceCts = null;

            // 既にStickyなら表示内容だけ合わせ直して終了
            if (StickyActive)
            {
                ForceMatchStickyGroupToScheme();
                return;
            }

            _stickyCts?.Cancel();
            _stickyCts?.Dispose();
            _stickyCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);

            // fire-and-forget（ここだけで Forget する。呼び出し側は Forget 禁止）
            _stickyTask = StickySequenceAsync(_stickyCts.Token);
            _stickyTask.Forget();
        }

        // もともと: public UniTask HideSticky()
        public void HideSticky()
        {
            if (!StickyActive) return;
            // StickySequenceAsync の finally でフェードアウト → 非表示
            _stickyCts?.Cancel();
        }


        // ===== 実装詳細 =====

        private async UniTask ShowCurrentDeviceHintSequenceAsync(float holdSeconds, CancellationToken lifeToken)
        {
            // ★ ここで直接 GameState を参照
            if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState != GameState.Gameplay)
                return;

            // 前回シーケンスを中断
            _sequenceCts?.Cancel();
            _sequenceCts?.Dispose();
            _sequenceCts = CancellationTokenSource.CreateLinkedTokenSource(lifeToken);
            var ct = _sequenceCts.Token;

            // どちらを表示するか決定
            bool showPad = ShouldPreferPad(_playerInput);
            var show = showPad ? _padGroup : _keyboardGroup;
            var hide = showPad ? _keyboardGroup : _padGroup;

            // 反対側は即座に消す
            HideImmediate(hide);

            // フェードイン → 待機 → フェードアウト
            try
            {
                await FadeInAsync(show, fadeInDuration, ct);
                await DelaySeconds(holdSeconds, ct);
                await FadeOutAsync(show, fadeOutDuration, ct);
                HideImmediate(show);
            }
            catch (OperationCanceledException)
            {
                HideImmediate(show);
            }
        }

        private async UniTask StickySequenceAsync(CancellationToken ct)
        {
            // GamePlay中のみ
            if (!IsGameplayNow()) return;

            // 現在スキームでどちらを見せるか
            bool showPad = ShouldPreferPad(_playerInput);
            var show = showPad ? _padGroup : _keyboardGroup;
            var hide = showPad ? _keyboardGroup : _padGroup;

            HideImmediate(hide);

            try
            {
                await FadeInAsync(show, fadeInDuration, ct);
                // ★ AFK中は出しっぱなし（キャンセルされるまで待機）
                await UniTask.WaitUntilCanceled(ct);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            finally
            {
                // 解除時は通常通りフェードアウト
                try
                {
                    await FadeOutAsync(show, fadeOutDuration, CancellationToken.None);
                }
                finally
                {
                    HideImmediate(show);
                }
            }
        }

        private bool ShouldPreferPad(PlayerInput playerInput)
        {
            string scheme = playerInput.currentControlScheme ?? "";
            bool preferPadByName = scheme.Contains("Gamepad") || scheme.Contains("Controller");

            bool hasGamepad = false;
            bool hasKeyboardOrMouse = false;
            IReadOnlyList<InputDevice> devices = playerInput.devices;
            foreach (var d in devices)
            {
                if (d is Gamepad) hasGamepad = true;
                if (d is Keyboard || d is Mouse) hasKeyboardOrMouse = true;
            }

            return preferPadByName || (hasGamepad && !hasKeyboardOrMouse);
        }

        private bool IsGameplayNow()
        {
            return GameStateManager.Instance != null
                   && GameStateManager.Instance.CurrentState == GameState.Gameplay;
        }

        private void CancelAndHideAll()
        {
            _sequenceCts?.Cancel();
            _stickyCts?.Cancel();
            HideImmediate(_padGroup);
            HideImmediate(_keyboardGroup);
        }

        // ===== フェード系ユーティリティ（UniTaskで実装） =====

        private async UniTask FadeInAsync(CanvasGroup group, float duration, CancellationToken ct)
        {
            if (group == null) return;
            group.gameObject.SetActive(true);
            group.alpha = 0f;
            await LerpAlphaAsync(group, 0f, 1f, duration, ct);
        }

        private async UniTask FadeOutAsync(CanvasGroup group, float duration, CancellationToken ct)
        {
            if (group == null) return;
            float from = group.alpha;
            await LerpAlphaAsync(group, from, 0f, duration, ct);
        }

        private async UniTask LerpAlphaAsync(CanvasGroup group, float from, float to, float duration, CancellationToken ct)
        {
            if (group == null) return;

            if (duration <= 0f)
            {
                group.alpha = to;
                return;
            }

            float elapsed = 0f;
            group.gameObject.SetActive(true);
            group.alpha = from;

            // Updateループでアルファを補間
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, t);
            }

            group.alpha = to;
        }

        private async UniTask DelaySeconds(float seconds, CancellationToken ct)
        {
            if (seconds <= 0f) return;
            var timing = useUnscaledTime ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), timing, PlayerLoopTiming.Update, ct);
        }

        // ===== 表示/非表示の即時切替 =====

        private static void HideImmediate(CanvasGroup group)
        {
            if (group == null) return;
            group.alpha = 0f;
            if (group.gameObject.activeSelf)
                group.gameObject.SetActive(false);
        }

        // ImageにCanvasGroupが無ければ追加して返す
        private static CanvasGroup EnsureCanvasGroup(Image img)
        {
            if (img == null) return null;
            var cg = img.GetComponent<CanvasGroup>();
            if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Sticky中にデバイスが変わった場合、即座に表示対象を切替（無フェード）</summary>
        private void ForceMatchStickyGroupToScheme()
        {
            bool showPad = ShouldPreferPad(_playerInput);
            var show = showPad ? _padGroup : _keyboardGroup;
            var hide = showPad ? _keyboardGroup : _padGroup;

            if (show != null)
            {
                show.gameObject.SetActive(true);
                show.alpha = 1f;
            }
            HideImmediate(hide);
        }
    }
}
