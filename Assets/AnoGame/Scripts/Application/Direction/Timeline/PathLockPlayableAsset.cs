using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class PathLockPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [Header("Target (single transform)")]
    public ExposedReference<Transform> target;     // ← 目的地

    [Header("Motion")]
    public float moveSpeed = 2.0f;                 // EventLockControl.MoveToPoint に渡す速度
    [Min(0f)] public float stopDistance = 0.05f;   // 〃

    [Header("Turn")]
    public TurnMode turnMode = TurnMode.FaceMove;
    public enum TurnMode { Keep, FaceMove, FaceTarget }
    public ExposedReference<Transform> lookAt;     // FaceTarget 用

    [Header("Lock")]
    public bool lockDuringClip = true;             // クリップ中だけ Begin/EndLock する

    public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<PathLockPlayableBehaviour>.Create(graph);
        var b = playable.GetBehaviour();

        // クリップ設定を Behaviour にコピー
        b.target        = target;
        b.moveSpeed     = moveSpeed;
        b.stopDistance  = stopDistance;
        b.turnMode      = turnMode;
        b.lookAt        = lookAt;
        b.lockDuringClip= lockDuringClip;

        return playable;
    }
}

public class PathLockPlayableBehaviour : PlayableBehaviour
{
    // クリップ設定（Mixer から参照）
    public ExposedReference<Transform> target;
    public float moveSpeed, stopDistance;
    public PathLockPlayableAsset.TurnMode turnMode;
    public ExposedReference<Transform> lookAt;
    public bool lockDuringClip;

    // ランタイム状態（このクリップで MoveToPoint を一度だけ発行するためのフラグ）
    [System.NonSerialized] public bool issuedOnce;
    [System.NonSerialized] public int  targetInstanceId;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        // クリップ入り直し時にリセット
        issuedOnce = false;
        targetInstanceId = 0;
    }
}
