using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using VContainer;
using AnoGame.Application.Input;                 // IInputActionProvider
using AnoGame.Application.Player.Interaction;
using UniRx;
using Cysharp.Threading.Tasks;    // IInteractable, InteractionOption

namespace AnoGame.Application.Player
{
    // 任意: Hide 専用の個別イベント（必要時のみ使用）
    public struct HideCanceled
    {
        public Transform Actor;
        public CancelReason Reason;
        public object Source;
        public HideCanceled(Transform actor, CancelReason reason, object source = null)
        { Actor = actor; Reason = reason; Source = source; }
    }

    public class InteractionController : MonoBehaviour
    {
        [Inject] private IInputActionProvider _inputProvider;

        [Header("Scan")]
        [SerializeField] private float scanRadius = 2.0f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private float viewAngle = 120f;
        [SerializeField] private float scanInterval = 0.1f;

        private InputAction _interact;
        private InputAction _cancel;            // ★追加
        private InteractionOption? _running;    // ★継続中アクションを保持

        private float _scanTimer;

        private readonly List<IInteractable> _candidates = new();
        private readonly List<InteractionOption> _optionsBuffer = new();

        private InteractionOption? _bestQuick;   // RequiresHold == false の最良
        private InteractionOption? _bestHold;    // RequiresHold == true  の最良

        // フィールド追加
        private bool _hasNearby;                           // 直近の「候補あり」状態
        private InteractionOption? _focused;               // 直近の“提示中/最有力” オプション



        [SerializeField] private bool debugLog = true;   // ★オン/オフ切替
        void D(string msg)
        {
            if (debugLog) Debug.Log($"[Interaction] {msg}", this);
        }

        private void Awake()
        {
            var playerMap = _inputProvider.GetPlayerActionMap();
            _interact = playerMap.FindAction("Interact", throwIfNotFound: true);
            _cancel = playerMap.FindAction("Cancel", throwIfNotFound: true); // ★追加
        }

        private void OnEnable()
        {
            _interact.performed += OnInteractPerformed;
            _cancel.performed += OnCancelPerformed; // ★追加

            // 必要なら: _inputProvider.SwitchToPlayer();
        }

        private void OnDisable()
        {

            _interact.performed -= OnInteractPerformed;
            _cancel.performed -= OnCancelPerformed; // ★追加
        }

        private void Update()
        {
            // check GameState
            if (GameStateManager.Instance.CurrentState != GameState.Gameplay)
            {
                // If we were previously interacting or had candidates, clear them immediately
                if (_hasNearby || _candidates.Count > 0 || _running.HasValue)
                {
                    ForceClearInteraction();
                }
                _scanTimer = 0f; // Reset timer so we scan immediately upon return (optional)
                return;
            }

            // 一定間隔でスキャン（負荷を抑える）
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= scanInterval)
            {
                _scanTimer = 0f;
                RefreshCandidates();
                ResolveBest();
            }
        }

        private void ForceClearInteraction()
        {
            if (_running.HasValue)
            {
                _running.Value.Cancel?.Invoke();
                UniRx.MessageBroker.Default.Publish(
                     new InteractionCanceled(transform, _running.Value.Kind, CancelReason.Interrupted, source: null));
                _running = null;
            }

            _candidates.Clear();
            _bestHold = null;
            _bestQuick = null;
            _optionsBuffer.Clear();

            if (_hasNearby)
            {
                _hasNearby = false;
                UniRx.MessageBroker.Default.Publish(new InteractablesNearbyChanged
                {
                    Actor = transform,
                    HasAny = false
                });
            }
            // Focus も外れた扱いにするなら InteractionFocusChanged(null) も投げるべきだが
            // Presenterは InteractablesNearbyChanged(false) で HideHint するので一旦OK
            // 必要に応じて追加
            UniRx.MessageBroker.Default.Publish(new InteractionFocusChanged { Current = null });
        }



        private void RefreshCandidates()
        {
            _candidates.Clear();

            var cols = Physics.OverlapSphere(transform.position, scanRadius, interactableLayer);

            foreach (var col in cols)
            {
                if (col.TryGetComponent<IInteractable>(out var it))
                    _candidates.Add(it);
            }

            // ★継続アクションの有効監視（距離で自然キャンセルなど）
            if (_running.HasValue && _running.Value.IsContinuous)
            {
                bool stillValid = _running.Value.IsValid?.Invoke() ?? true;
                if (!stillValid)
                {
                    D("Continuous canceled by validity loss");
                    // コールバック
                    _running.Value.Cancel?.Invoke();
                    // 汎用キャンセルを通知
                    UniRx.MessageBroker.Default.Publish(
                        new InteractionCanceled(transform, _running.Value.Kind, CancelReason.Distance, source: null));
                    // 任意: 個別（Hide 等）を飛ばしたい場合はここで発行
                    _running = null;
                }
            }

        }

        private void ResolveBest()
        {
            _bestQuick = null;
            _bestHold = null;

            float bestQuickScore = float.NegativeInfinity;
            float bestHoldScore = float.NegativeInfinity;

            int optionsTotal = 0;

            foreach (var it in _candidates)
            {
                var s = it.Score(transform);
                if (s <= float.NegativeInfinity)
                {
                    D($"Score: {((MonoBehaviour)it).name} rejected (-∞)");
                    continue;
                }

                _optionsBuffer.Clear();
                if (!it.TryBuildOptions(transform, _optionsBuffer)) continue;

                optionsTotal += _optionsBuffer.Count;

                foreach (var op in _optionsBuffer)
                {
                    var weight = s + op.Priority;
                    if (op.RequiresHold)
                    {
                        if (weight > bestHoldScore) { bestHoldScore = weight; _bestHold = op; }
                    }
                    else
                    {
                        if (weight > bestQuickScore) { bestQuickScore = weight; _bestQuick = op; }
                    }
                }
            }

            // ★解決結果のサマリ
            D($"Resolve: options={optionsTotal}, bestQuick={(_bestQuick.HasValue ? _bestQuick.Value.Prompt : "null")}, bestHold={(_bestHold.HasValue ? _bestHold.Value.Prompt : "null")}");

            var hasAny = optionsTotal > 0;

            // 変更: 変化チェックをやめて、毎回 Publish する
            _hasNearby = hasAny;
            UniRx.MessageBroker.Default.Publish(new InteractablesNearbyChanged
            {
                Actor = transform,
                HasAny = _hasNearby
            });
            D($"Nearby -> {_hasNearby}");
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (GameStateManager.Instance.CurrentState != GameState.Gameplay) return;

            D($"Input: Interact performed ({ctx.interaction?.GetType().Name ?? "Press"})");



            // 実行直前に再解決（離れてたら実行しないため）
            ResolveBest();

            bool isHold = ctx.interaction is HoldInteraction;
            var op = isHold ? (_bestHold ?? _bestQuick) : (_bestQuick ?? _bestHold);

            if (!op.HasValue) { D("Execute skipped: no valid candidate"); return; }

            // 実行
            op.Value.Execute?.Invoke();

            // 継続型なら監視に登録
            if (op.Value.IsContinuous) _running = op.Value;
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            // _session?.RequestCancel();
            D("Input: Cancel performed");
            UniRx.MessageBroker.Default.Publish(
                    new InteractionCanceled(transform, InteractionKind.Hide, CancelReason.UserRequest, source: null));
            D($"_running.HasValue-{_running.HasValue}");
            if (_running.HasValue)
            {
                var r = _running.Value;
                // 明示キャンセル
                r.Cancel?.Invoke();

                // 汎用イベント
                UniRx.MessageBroker.Default.Publish(
                    new InteractionCanceled(transform, r.Kind, CancelReason.UserRequest, source: null));

                // 任意: 個別イベント（例：HideCanceled）
                if (r.Kind == InteractionKind.Hide)
                {
                    UniRx.MessageBroker.Default.Publish(
                        new HideCanceled(transform, CancelReason.UserRequest));
                }

                _running = null;
            }
            else
            {
                // 実行中がない場合でも「汎用キャンセル押下」を通知したいなら、ここでPublishしてもOK
            }
        }

        private void OnInteractCanceled(InputAction.CallbackContext ctx)
        {
            // Hold 中断で何かしたい場合はここに
        }

#if UNITY_EDITOR
        // デバッグ用の可視化（任意）
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, scanRadius);

            var right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
            var left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
            Gizmos.DrawRay(transform.position, right * scanRadius);
            Gizmos.DrawRay(transform.position, left * scanRadius);
        }
#endif
    }
}
