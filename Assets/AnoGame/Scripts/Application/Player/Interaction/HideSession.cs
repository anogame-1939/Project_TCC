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

        public async UniTask RunAsync(CancellationToken external)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(external, _cts.Token);
            var token = linked.Token;

            IsActive = true;

            // 占有（他プレイヤー/他処理と競合しないように）
            if (!_spot.TryReserve(_actor))
            { IsActive = false; return; }

            try
            {
                // 入口→隠れる（移動経路・演出は HideSpotZone 側に委譲）
                await _spot.MoveIntoAsync(_actor, token);
                await _spot.EnterHideAsync(_actor, token);

                // 隠れ中：出る/キャンセル/無効化 いずれかまで待機
                await UniTask.WaitUntil(
                    () => _exitRequested || _cancelRequested || !IsValid(),
                    cancellationToken: token
                );

                if (_exitRequested && (_danger?.Invoke() != true) && IsValid())
                {
                    await _spot.ExitHideAsync(_actor, token);   // 退出演出を完了
                    // ここで「出た」イベント等をPublishしたい場合はSpot側/ここで実施
                }
                else
                {
                    // 中断or無効化時：必要なら軽い演出だけ
                    await _spot.CancelHideAsync(_actor, token);
                }
            }
            finally
            {
                _spot.Release(_actor); // 占有解除＋カメラ/入力復帰など
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
