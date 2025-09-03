// AnoGame.Application.Player.Interaction
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AnoGame.Application.Player.Interaction
{
    public sealed class HideSession : IInteractionSession
    {
        public bool IsActive { get; private set; }

        private readonly Transform _actor;
        private readonly HideSpotZone _spot;
        private readonly System.Func<bool> _danger;     // 危険時は退出禁止 等の判定(任意)
        private readonly CancellationTokenSource _cts = new();

        private volatile bool _exitRequested;
        private volatile bool _cancelRequested;

        public HideSession(Transform actor, HideSpotZone spot, System.Func<bool> dangerCheck = null)
        { _actor = actor; _spot = spot; _danger = dangerCheck; }

        enum Phase { Idle, Approaching, Entering, Hidden, Exiting, Canceled }
        Phase _phase = Phase.Idle;

        public async UniTask RunAsync(CancellationToken ct)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            var token = linked.Token;

            IsActive = true;

            if (!_spot.TryReserve(_actor))
            { IsActive = false; return; }

            try
            {
                _phase = Phase.Approaching;
                await _spot.MoveIntoAsync(_actor, token);   // ← token を使う

                _phase = Phase.Entering;
                await _spot.EnterHideAsync(_actor, token);  // ← token を使う

                _phase = Phase.Hidden;

                // ★ここで待つ：出る / キャンセル / 無効化 のいずれかまで
                await UniTask.WaitUntil(
                    () => _exitRequested || _cancelRequested || !IsValid(),
                    cancellationToken: token
                );

                // ★分岐：本当に出る？ それとも中断？
                if (_exitRequested && (_danger?.Invoke() != true) && IsValid())
                {
                    await _spot.ExitHideAsync(_actor, token);   // 退出演出を完了
                }
                else
                {
                    await _spot.CancelHideAsync(_actor, token); // 中断演出（軽い解除）
                }
            }
            finally
            {
                _phase = Phase.Canceled;
                _spot.Release(_actor);   // ★ここでだけ EndLock（1回目で即解除されなくなる）
                IsActive = false;
            }
        }


        public bool IsValid() => _spot != null && _spot.StillValidFor(_actor);

        public void RequestExit()   => _exitRequested = true;
        public void RequestCancel() => _cancelRequested = true;

        public bool TryBuildRuntimeOptions(System.Collections.Generic.List<InteractionOption> buf)
        {
            if (!IsActive) return false;
            buf.Add(new InteractionOption {
                Kind = InteractionKind.Hide,
                Prompt = "出る",
                Priority = 1000,
                RequiresHold = false,
                Execute = () => RequestExit(),
                IsContinuous = false
            });
            return true;
        }

        public void Dispose() { _cts.Cancel(); }
    }
}
