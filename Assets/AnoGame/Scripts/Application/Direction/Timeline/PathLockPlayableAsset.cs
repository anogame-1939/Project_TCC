using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class PathLockPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [Header("Target (single transform)")]
    public ExposedReference<Transform> target;

    [Header("Motion")]
    public float moveSpeed = 2.0f;
    [Min(0f)] public float stopDistance = 0.05f;

    [Header("Turn")]
    public TurnMode turnMode = TurnMode.FaceMove;
    public enum TurnMode { Keep, FaceMove, FaceTarget }
    public ExposedReference<Transform> lookAt;     // ★ ここは Exposed のままでOK（解決は下で）

    [Header("Lock")]
    public bool lockDuringClip = true;

    public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PathLockPlayableBehaviour>.Create(graph);
        var b = playable.GetBehaviour();

        var resolver = graph.GetResolver();

        // クリップ設定を Behaviour にコピー（★ Resolve 済みを渡す）
        b.target        = target.Resolve(resolver);
        b.moveSpeed     = moveSpeed;
        b.stopDistance  = stopDistance;
        b.turnMode      = turnMode;
        b.lookAt        = lookAt.Resolve(resolver);   // ★ 追加：Resolve して Transform を渡す
        b.lockDuringClip= lockDuringClip;

        return playable;
    }
}

public class PathLockPlayableBehaviour : PlayableBehaviour
{
    // クリップ設定（Mixer から参照）
    public Transform target;
    public float moveSpeed, stopDistance;
    public PathLockPlayableAsset.TurnMode turnMode;
    public Transform lookAt;                // ★ ExposedReference → Transform に変更
    public bool lockDuringClip;

    // 一度打ちや重複呼びを避けたい場合に使えるキャッシュ（必要に応じて）
    [System.NonSerialized] public bool issuedOnce;
    [System.NonSerialized] public int  targetInstanceId;
    [System.NonSerialized] public PathLockPlayableAsset.TurnMode lastTurnMode;
    [System.NonSerialized] public int  lastLookAtInstanceId;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        issuedOnce = false;
        targetInstanceId = target ? target.GetInstanceID() : 0;
        lastTurnMode = (PathLockPlayableAsset.TurnMode)(-1);
        lastLookAtInstanceId = lookAt ? lookAt.GetInstanceID() : 0;
    }
}
