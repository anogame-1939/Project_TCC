using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Splines;

[System.Serializable]
public class PathLockPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    // シーン上のパス（Unity Splines）
    public ExposedReference<SplineContainer> path;

    [Header("Path range (0..1)")]
    [Range(0,1)] public float from = 0f;
    [Range(0,1)] public float to   = 1f;

    [Header("Motion")]
    public float moveSpeed = 2.0f;                 // EventLockControl.MoveToPoint に渡す速度
    [Min(0f)] public float stopDistance = 0.05f;   // 〃
    public AnimationCurve easing = AnimationCurve.Linear(0,0, 1,1);

    
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

        b.path = path;
        b.from = Mathf.Clamp01(from);
        b.to   = Mathf.Clamp01(to);
        b.moveSpeed = moveSpeed;
        b.stopDistance = stopDistance;
        b.easing = easing ?? AnimationCurve.Linear(0,0,1,1);
        b.turnMode = turnMode;
        b.lookAt = lookAt;
        b.lockDuringClip = lockDuringClip;

        return playable;
    }
}

public class PathLockPlayableBehaviour : PlayableBehaviour
{
    // クリップ設定（Mixer から参照）
    public ExposedReference<SplineContainer> path;
    public float from, to;
    public float moveSpeed, stopDistance;
    public AnimationCurve easing;
    public PathLockPlayableAsset.TurnMode turnMode;
    public ExposedReference<Transform> lookAt;
    public bool lockDuringClip;
}
