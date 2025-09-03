using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AnoGame.Application.Player.Interaction
{
    public interface IInteractionSession : IDisposable
    {
        bool IsActive { get; }
        bool IsValid();                                  // 継続可否（距離/占有/壊れ 等）
        UniTask RunAsync(CancellationToken external);    // 入口→隠れる→退出/中断 まで完結
        void RequestExit();                              // 「出る」要求（Interact）
        void RequestCancel();                            // 中断要求（Cancel）
        bool TryBuildRuntimeOptions(List<InteractionOption> buffer); // UIに「出る」を合成
    }
}
