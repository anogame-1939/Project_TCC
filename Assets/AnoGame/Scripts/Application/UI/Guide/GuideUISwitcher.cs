using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using AnoGame.Application; // GameStateManager 参照

namespace AnoGame.UI.Guide
{
    public sealed class GuideUISwitcher : MonoBehaviour
    {
        [Header("Base UI")]
        [SerializeField] private Image baseImage;         // ← ベースとなる1枚だけ
        [SerializeField] private Sprite padSprite;        // パッド用スプライト
        [SerializeField] private Sprite keyboardSprite;   // キーボード用スプライト

        [Header("Fade")]
        [SerializeField] private float fadeInDuration  = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private bool  useUnscaledTime = true;

        [Header("Rules")]
        [SerializeField] private bool showOnlyInGameplay = true; // Gameplay 中のみ表示

        private PlayerInput _playerInput;
        private CanvasGroup _group;

        private CancellationTokenSource _lifeCts;   // Destroy まで
        private CancellationTokenSource _fadeCts;   // 進行中フェード

        private void Awake()
        {
            _lifeCts = new CancellationTokenSource();
        }

        private void Start()
        {
            // 依存チェック
            if (baseImage == null)
            {
                Debug.LogError("[GuideUISwitcher] baseImage が未設定です。");
                enabled = false; return;
            }
            if (padSprite == null || keyboardSprite == null)
            {
                Debug.LogWarning("[GuideUISwitcher] padSprite / keyboardSprite のいずれかが未設定です。");
            }

            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[GuideUISwitcher] PlayerInput が見つかりません。");
                enabled = false; return;
            }

            _group = EnsureCanvasGroup(baseImage);
            _group.blocksRaycasts = false;
            _group.interactable  = false;

            HideImmediate(); // 初期は非表示
        }

        private void OnEnable()
        {
            if (_playerInput == null) _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput != null)
                _playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDisable()
        {
            if (_playerInput != null)
                _playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnDestroy()
        {
            _fadeCts?.Cancel(); _fadeCts?.Dispose(); _fadeCts = null;
            _lifeCts?.Cancel(); _lifeCts?.Dispose(); _lifeCts = null;
        }

        // ===== Public API =====

        /// <summary>AFKになったら呼ぶ：現在スキームのスプライトで表示（フェードイン）。</summary>
        public void Show()
        {
            if (!CanShowNow()) return;

            // 表示中でも“即”スプライト差し替え（フェード状態は維持）
            ApplySpriteForCurrentScheme();

            // 既にフル表示なら何もしない
            if (_group.gameObject.activeSelf && _group.alpha >= 1f) return;

            // フェードイン
            StartFade(toVisible: true);
        }

        /// <summary>AFK解除で呼ぶ：フェードアウトして非表示。</summary>
        public void Hide()
        {
            // 非表示済みなら何もしない
            if (!_group.gameObject.activeSelf && _group.alpha <= 0f) return;

            StartFade(toVisible: false);
        }

        /// <summary>即時に完全非表示。</summary>
        public void HideImmediate()
        {
            _fadeCts?.Cancel(); // 進行中のフェード停止
            _group.alpha = 0f;
            if (_group.gameObject.activeSelf) _group.gameObject.SetActive(false);
        }

        // ===== Internals =====

        private void OnControlsChanged(PlayerInput pi)
        {
            if (pi != _playerInput) return;

            // 「表示中なら」スプライトを即差し替え（フェードは触らない）
            if (_group != null && (_group.gameObject.activeSelf || _group.alpha > 0f))
            {
                ApplySpriteForCurrentScheme();
            }
            // 非表示時は何もしない（次回 Show で反映）
        }

        private void StartFade(bool toVisible)
        {
            // Gameplay から外れたら出さない
            if (toVisible && !CanShowNow()) return;

            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(_lifeCts.Token);
            var ct = _fadeCts.Token;

            if (toVisible)
            {
                // 表示用セットアップ
                ApplySpriteForCurrentScheme(); // 念のため再適用
                _group.gameObject.SetActive(true);
                FadeRoutine(_group, _group.alpha, 1f, fadeInDuration, ct).Forget();
            }
            else
            {
                FadeRoutine(_group, _group.alpha, 0f, fadeOutDuration, ct).Forget();
            }
        }

        private async UniTask FadeRoutine(CanvasGroup g, float from, float to, float duration, CancellationToken ct)
        {
            if (g == null) return;

            if (duration <= 0f)
            {
                g.alpha = to;
                if (to <= 0f && g.gameObject.activeSelf) g.gameObject.SetActive(false);
                return;
            }

            float elapsed = 0f;
            g.alpha = from;
            g.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt;
                g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            }

            g.alpha = to;
            if (to <= 0f && g.gameObject.activeSelf) g.gameObject.SetActive(false);
        }

        private void ApplySpriteForCurrentScheme()
        {
            if (baseImage == null) return;

            bool isPad = ShouldPreferPad(_playerInput);
            var next = isPad ? padSprite : keyboardSprite;

            // スプライトが同一なら触らない（不要な再描画回避）
            if (baseImage.sprite == next) return;

            baseImage.sprite = next;
            baseImage.SetNativeSize(); // 必要ならON。固定サイズ運用なら削除OK
        }

        private static CanvasGroup EnsureCanvasGroup(Image img)
        {
            var cg = img.GetComponent<CanvasGroup>();
            if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        private bool CanShowNow()
        {
            if (!showOnlyInGameplay) return true;

            return GameStateManager.Instance != null
                && GameStateManager.Instance.CurrentState == GameState.Gameplay;
        }

        private static bool ShouldPreferPad(PlayerInput playerInput)
        {
            if (playerInput == null) return false;

            // 1) スキーム名で判定
            string scheme = playerInput.currentControlScheme ?? string.Empty;
            if (scheme.Contains("Gamepad") || scheme.Contains("Controller"))
                return true;

            // 2) デバイス列挙の保険
            bool hasPad = false, hasKM = false;
            IReadOnlyList<InputDevice> devices = playerInput.devices;
            foreach (var d in devices)
            {
                if (d is Gamepad) hasPad = true;
                if (d is Keyboard || d is Mouse) hasKM = true;
            }
            return hasPad && !hasKM;
        }
    }
}
