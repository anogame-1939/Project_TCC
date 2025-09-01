using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Player.Interaction
{
    public enum InteractionKind { Pickup, Hide, Inspect }

    public struct InteractionOption
    {
        public InteractionKind Kind;
        public string Prompt;
        public int Priority;
        public bool RequiresHold;

        public bool IsContinuous;                   // 継続型？
        public System.Action Execute;               // 実行
        public System.Action Cancel;                // ★ユーザーキャンセル時に呼ぶ（任意）
        public System.Func<bool> IsValid;           // ★継続中の有効判定（距離/角度/LoS等 任意）
    }

    public enum CancelReason { UserRequest, Distance, Interrupted }

    public struct InteractionCanceled
    {
        public Transform Actor;
        public InteractionKind Kind;           // Hide / Pickup / Inspect ...
        public CancelReason Reason;
        public object Source;                  // ゾーン/アイテムなど(任意)

        public InteractionCanceled(Transform actor, InteractionKind kind, CancelReason reason, object source = null)
        {
            Actor = actor; Kind = kind; Reason = reason; Source = source;
        }
    }

    public interface IInteractable
    {
        // 候補をbufferに追加（0件でもOK）。true=候補あり。
        bool TryBuildOptions(Transform actor, List<InteractionOption> buffer);

        // 解決用スコア（距離/角度/視線などを反映）。高い方が優先。
        float Score(Transform actor);

        // プロンプトのアンカー（頭上など）
        Vector3 WorldUiAnchor { get; }
    }
}
