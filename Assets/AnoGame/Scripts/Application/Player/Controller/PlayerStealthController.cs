using AnoGame.Application.Direction;
using AnoGame.Application.Player.Interaction;
using UniRx;
using UnityEngine;

namespace AnoGame.Application.Player.Controller
{

    public class PlayerStealthController : MonoBehaviour
    {
        [SerializeField] private CameraModeDirector _cameraDirector;
        private readonly CompositeDisposable _disposables = new();

        private void OnEnable()
        {
            MessageBroker.Default
                .Receive<HideRequested>()
                .Subscribe(msg =>
                {
                    // ここで EventLockControl を呼ぶ
                    // EventLockControl.Begin(msg.Actor, /* 任意のオプション */);
                    Debug.Log($"[Stealth] HideRequested: {msg.Actor.name} -> {msg.Spot.name}", this);

                    _cameraDirector.ToPerspective();
                })
                .AddTo(_disposables);

            // 任意: 個別イベントをそのまま購読
            MessageBroker.Default
                .Receive<HideCanceled>()
                .Subscribe(e =>
                {
                    Debug.Log($"[Stealth] HideCanceled: {e.Actor.name}, Reason: {e.Reason}", this);
                    // 同上（より限定的に）
                    _cameraDirector.ToOrthographic();
                })
                .AddTo(_disposables);

            MessageBroker.Default
                .Receive<InteractionCanceled>()
                .Subscribe(e =>
                {
                    Debug.Log($"[Stealth] InteractionCanceled: {e.Actor.name}, Kind: {e.Kind}, Reason: {e.Reason}", this);
                    
                    _cameraDirector.ToOrthographic();
                })
                .AddTo(_disposables);
        }



        private void OnDisable() => _disposables.Clear();
    }
}