using AnoGame.Application.Direction;
using AnoGame.Application.Enemy.AI;
using AnoGame.Application.Player.Interaction;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace AnoGame.Application.Player.Controller
{
    // UnityEvent の引数付き版（インスペクタで使いやすくするためのラッパ）
    [System.Serializable] public class TransformEvent : UnityEvent<Transform> { }
    [System.Serializable] public class HideSpotEvent : UnityEvent<HideSpotZone> { }
    [System.Serializable] public class CancelReasonEvent : UnityEvent<CancelReason> { }

    public class PlayerStealthController : MonoBehaviour, IVisibleTarget
    {
        private readonly CompositeDisposable _disposables = new();

        [Header("Unity Events / Hide Start")]
        [SerializeField] private UnityEvent onHideStarted;                   // 引数なし
        [SerializeField] private TransformEvent onHideStartedActor;          // Actor を渡す
        [SerializeField] private HideSpotEvent onHideStartedSpot;            // Spot を渡す

        [Header("Unity Events / Hide Cancel")]
        [SerializeField] private UnityEvent onHideCanceled;                  // 引数なし
        [SerializeField] private TransformEvent onHideCanceledActor;         // Actor
        [SerializeField] private CancelReasonEvent onHideCanceledReason;     // 理由

        // 同フレーム内の二重発火を抑止するためのフラグ
        private bool _hideCanceledThisFrame;

        // IVisibleTarget implementation
        public Transform GetTransform() => transform;
        public bool IsHidden { get; private set; }

        private void OnEnable()
        {
            MessageBroker.Default
                .Receive<HideRequested>()
                .Subscribe(msg =>
                {
                    Debug.Log($"[Stealth] HideRequested: {msg.Actor.name} -> {msg.Spot.name}", this);

                    // UnityEvent 発火（開始）
                    onHideStarted?.Invoke();
                    onHideStartedActor?.Invoke(msg.Actor);
                    onHideStartedSpot?.Invoke(msg.Spot);

                    IsHidden = true;

                })
                .AddTo(_disposables);

            // 明示的な HideCanceled（ゾーン側からの個別イベント）
            MessageBroker.Default
                .Receive<HideCanceled>()
                .Subscribe(e =>
                {
                    Debug.Log($"[Stealth] HideCanceled: {e.Actor.name}, Reason: {e.Reason}", this);
                    _hideCanceledThisFrame = true;  // 同フレームの InteractionCanceled を抑止

                    // UnityEvent 発火（キャンセル）
                    onHideCanceled?.Invoke();
                    onHideCanceledActor?.Invoke(e.Actor);
                    onHideCanceledReason?.Invoke(e.Reason);

                    IsHidden = false;
                })
                .AddTo(_disposables);

            // 汎用キャンセル（InteractionController からの通知）
            // Hide 以外で誤爆させないために Kind==Hide に絞る
            MessageBroker.Default
                .Receive<InteractionCanceled>()
                .Where(e => e.Kind == InteractionKind.Hide)
                .Subscribe(e =>
                {
                    Debug.Log($"[Stealth] InteractionCanceled(Hide): {e.Actor.name}, Reason: {e.Reason}", this);

                    // 直前フレームで HideCanceled が来ていれば二重発火を避ける
                    if (_hideCanceledThisFrame) return;

                    onHideCanceled?.Invoke();
                    onHideCanceledActor?.Invoke(e.Actor);
                    onHideCanceledReason?.Invoke(e.Reason);

                    IsHidden = false;
                })
                .AddTo(_disposables);
        }

        private void LateUpdate()
        {
            // フレーム終端でフラグをリセット
            _hideCanceledThisFrame = false;
        }

        private void OnDisable() => _disposables.Clear();
    }
}
